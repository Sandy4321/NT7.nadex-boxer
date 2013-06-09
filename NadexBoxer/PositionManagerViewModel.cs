using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace NadexBoxer
{
    public class PositionManagerViewModel : INotifyPropertyChanged
    {
        private readonly object                     _lockCookie     = new object();
        private ObservableCollection<NadexPosition> _positions      = new ObservableCollection<NadexPosition>();
        private string                              _positionStorageFile;
        private NadexPosition                       _selectedItem;
                
        private RoutedCommand _addPositionCommand       = new RoutedCommand("AddNewPositionCommand", typeof(PositionManagerViewModel));
        private RoutedCommand _removePositionCommand    = new RoutedCommand("RemovePositionCommand", typeof(PositionManagerViewModel));

        public RoutedCommand AddPositionCommand    { get { return _addPositionCommand; } }
        public RoutedCommand RemovePositionCommand { get { return _removePositionCommand; } }

        public ObservableCollection<NadexPosition> Positions { get { return _positions; } }

        private static ContractType[] _contractTypes;
        private static PositionDirection[] _marketPositionTypes;

        public static IEnumerable<ContractType> ContractTypes           
        { 
            get 
            {
                if (_contractTypes == null)
                    _contractTypes = (ContractType[]) Enum.GetValues(typeof(ContractType));
                return _contractTypes; 
            } 
        }

        public static IEnumerable<PositionDirection>  MarketPositionTypes
        { 
            get 
            { 
                if (_marketPositionTypes == null)
                    _marketPositionTypes = (PositionDirection[]) Enum.GetValues(typeof(PositionDirection));
                return _marketPositionTypes;
            } 
        }       

        private NinjaTrader.Indicator.NadexBoxer _indicator;
        public NinjaTrader.Indicator.NadexBoxer Indicator 
        { 
            get { return _indicator; }
            set 
            {
                _indicator = value;
                if ( _indicator.Instrument != null)
                    _positionStorageFile = string.Format(CultureInfo.InvariantCulture, "ndxPos_{0}_{1}" + _indicator.Name + _indicator.Instrument.FullName);
            }
        }

        public bool IsEditGridVisible
        {
            get { return SelectedItem != null; }
        }

        public bool IsSpreadEditEnabled
        {
            get { return SelectedItem != null && SelectedItem.ContractType == ContractType.Spread; } 
        }

        public bool IsStrikeEditEnabled
        {
            get { return !IsSpreadEditEnabled; } 
        }

        public NadexPosition SelectedItem 
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value)
                    return;

                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
                OnPropertyChanged("IsEditGridVisible");
                OnPropertyChanged("IsSpreadEditEnabled");
                OnPropertyChanged("IsStrikeEditEnabled");
            }
        }

        // note: realtime PnL only only makes sense for spreads - binary PnL will only show settlement PnL! 
        public void CalculatePnL(double lastPriceUpdate)
        {
            if (_positions.Count == 0)
                return;
            foreach (var position in _positions)
            {
                if (position.ContractType == ContractType.Binary)
                {
                    // this is easy for binary settlement. they always settle for either $0 or $100.
                    if (position.MarketPosition == PositionDirection.Buy)
                    {
                        if (position.StrikePrice < lastPriceUpdate)
                            position.CurrentPnL = 100d - position.PricePaid;
                        else
                            position.CurrentPnL = -(position.PricePaid);  // outta your premium pal!
                    }
                    else
                    {
                        // seller only makes the premium paid to them
                        if (position.StrikePrice < lastPriceUpdate)
                            position.CurrentPnL = position.PricePaid;
                        else
                        {
                            // price paid should never be > 100 unless somenoe doesnt know wtf to put in
                            position.CurrentPnL = -(100d - Math.Min(position.PricePaid, 100));
                        }
                    }
                }
                else
                {
                    // spreads require a little math to derive the range, where each tick is worth $1
                    // eg. a spread of $300 value, you got to calculate price paid from floor/ceil and drop divisor to get $ amount
                    // TODO:
                }
            }
        }

        public void OnAddPositionCommand(object sender, EventArgs e)
        {
            NadexPosition newPosition = new NadexPosition { DisplayName = "New position..", Expiration = DateTime.Now };
            Positions.Add(newPosition);
            SelectedItem = newPosition;

            SavePositionsToXml();
        }

        public void OnRemovePositionCommand(object sender, EventArgs e)
        {
            // this should never be possible
            if (SelectedItem == null)
                return;
            Positions.Remove(SelectedItem);
            SelectedItem = null;            

            SavePositionsToXml();
        }

		public void OnContractTypeSelectionChanged()
		{
			OnPropertyChanged("IsSpreadEditEnabled");
			OnPropertyChanged("IsStrikeEditEnabled");
		}

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public PositionManagerViewModel(PositionManager owner) //, NinjaTrader.Indicator.NadexBoxer parentIndicator)
        {
            //Positions.Add(new NadexPosition { DisplayName = "Spread 1", IsActive = true, Expiration = DateTime.Now.AddHours(1), SpreadCeiling = 1425, SpreadFloor = 1423,
            //    Quantity = 1, PricePaid = 1423.50, CurrentPnL = 12.00d });
            owner.CommandBindings.Add(new CommandBinding(AddPositionCommand, OnAddPositionCommand));
            owner.CommandBindings.Add(new CommandBinding(RemovePositionCommand, OnRemovePositionCommand, (o,e) => e.CanExecute = _selectedItem != null ));
            owner.Dispatcher.BeginInvoke(new Action(() => LoadPositionsFromXml()), DispatcherPriority.Background);
        }

		/// <summary>
		/// plots all active positions on given chart graphics handle
		/// </summary>
		public void PlotAll()
		{
            if (Indicator == null)
                return;
            //throw new ArgumentException("Need indicator to plot for");

			foreach (var position in Positions.Where(p => p.IsActive))
			{
                // this is best for intraday expires. but support daily/weekly absolute expirations too
                // means user putting in shit date gets shit plots
			    //DateTime endDate = owner.Time[0].Date.AddHours(position.Expiration.Hour).AddMinutes(position.Expiration.Minute);
                var endDate = position.Expiration;
				if (position.ContractType == ContractType.Binary)
				{
					// for a binary, just draw a rectangle for breakeven profit / loss zones
                    Indicator.DrawRectangle(position.DisplayName+"_profit", false, Indicator.Time[0], position.StrikePrice, endDate, position.StrikePrice*2, Color.Transparent, Color.Blue, 2);
					Indicator.DrawRectangle(position.DisplayName+"_loss", false, Indicator.Time[0], position.StrikePrice, endDate, Math.Min(0, -(position.StrikePrice*2)), Color.Transparent, Color.Red, 2);
				}
				else
				{

				}
			}
		}

        public void SavePositionsToXml()
        {            
            lock (_lockCookie)
            {
                //Trace.Assert(!string.IsNullOrEmpty(_positionStorageFile));
                if (string.IsNullOrEmpty(_positionStorageFile))
                    return;
                try 
                {
                    var isolatedStorageFileStream = new IsolatedStorageFileStream(_positionStorageFile, FileMode.Create);
                    var xmlSerializer = new XmlSerializer(typeof(ObservableCollection<NadexPosition>));
                    xmlSerializer.Serialize(isolatedStorageFileStream, _positions);                   
                }
                catch (XmlException ex)
                {
                    Trace.WriteLine("NadexBoxer: Failed to serialize positions Exception = " + ex.Message);
                }
                catch (IOException ex)
                {
                    Trace.WriteLine("NadexBoxer: Failed to serialize positions. Exception = " + ex.Message);
                }
            }
        }

        public void LoadPositionsFromXml()
        {
            lock (_lockCookie)
            {
                //Trace.Assert(!string.IsNullOrEmpty(_positionStorageFile));
                if (string.IsNullOrEmpty(_positionStorageFile))
                    return;

                try 
                {
                    IsolatedStorageFileStream isolatedStorageFileStream = new IsolatedStorageFileStream(_positionStorageFile, FileMode.Open);
                    _positions = (ObservableCollection<NadexPosition>) new XmlSerializer(typeof (ObservableCollection<NadexPosition>)).Deserialize(isolatedStorageFileStream);
                }
                catch (FileNotFoundException){} // not uncommon for first load. dont care because that means there are no positions for this instrument
            }
        }
    }
}
