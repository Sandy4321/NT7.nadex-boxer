using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NadexBoxer
{
    // use this over Cbi.MarketPosition so there is no flat
    public enum PositionDirection { Buy, Sell };
    public enum ContractType { Spread, Binary };

    public class NadexPosition : INotifyPropertyChanged
    {
        private ContractType        _contractType;
        private double              _currentPnL;
        private string              _displayName;
        private DateTime            _expiration;
        private bool                _isActive;
        private PositionDirection   _position;
        private double              _strikePrice;
        private double              _spreadCeiling;
        private double              _spreadFloor;
        private double              _pricePaid;
        private int                 _quantity;
        
        public ContractType ContractType   
        { 
            get { return _contractType; }
            set
            {
                _contractType = value;
                OnPropertyChanged("ContractType");
            }
        }

        public double CurrentPnL
        {
            get { return _currentPnL; }        
            set
            {
                _currentPnL = value;
                OnPropertyChanged("CurrentPnL");
            }
        }

        public string DisplayName     
        { 
            get { return _displayName; }
            set
            {
                _displayName = value;
                OnPropertyChanged("DisplayName");
            }
        }

        public DateTime Expiration
        { 
            get { return _expiration; }
            set
            {
                _expiration = value;
                OnPropertyChanged("Expiration");
            }
        }

        public bool IsActive
        { 
            get { return _isActive; }
            set 
            {
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        public PositionDirection MarketPosition  
        { 
            get { return _position; }
            set
            {
                _position = value; 
                OnPropertyChanged("MarketPosition");
            }
        }

        public double SpreadCeiling
        { 
            get { return _spreadCeiling; }
            set
            {
                _spreadCeiling = value;
                OnPropertyChanged("SpreadCeiling");
            }
        }

        public double SpreadFloor 
        { 
            get { return _spreadFloor; }
            set
            {
                _spreadFloor = value;
                OnPropertyChanged("SpreadFloor");
            }
        }       

        public double StrikePrice 
        { 
            get { return _strikePrice; }
            set
            {
                _strikePrice = value;
                OnPropertyChanged("StrikePrice");
            }
        }

        public double PricePaid
        { 
            get { return _pricePaid; }
            set
            {
                _pricePaid = value;
                OnPropertyChanged("PricePaid");
            }
        }

        public int Quantity 
        { 
            get { return _quantity; }
            set
            {
                _quantity = value;
                OnPropertyChanged("Quantity");
            }
        }   

        public NadexPosition()
        {
            DisplayName = "Unset Display Name";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
