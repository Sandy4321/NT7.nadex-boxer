using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NadexBoxer;

namespace NinjaTrader.Indicator
{
    // actual indicator
    public class NadexBoxer : IndicatorBase
    {
        private PositionManager _positionManager;
        protected override void Initialize()
        {
#if DEBUG
            Print("NadexBoxer::Initialize");
#endif
            Overlay = true;
            Panel = 0;
            CalculateOnBarClose = false;
        }

        protected override void OnBarUpdate()
        {
            if (_positionManager == null)
                return;
            _positionManager.ViewModel.CalculatePnL(Close[0]);
        }

        protected override void OnStartUp()
        {
#if DEBUG
            Print("NadexBoxer::OnStartUp");
#endif
            // you have to do this or else keyboard input wont work in the wpf control!! jesus christ            
            _positionManager = new PositionManager();
            _positionManager.Closing += (o,e) => _positionManager = null;
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(_positionManager);
            _positionManager.Show();
            _positionManager.ViewModel.Indicator = this;
            _positionManager.ViewModel.LoadPositionsFromXml();
        }

		public override void Plot(System.Drawing.Graphics graphics, System.Drawing.Rectangle bounds, double min, double max)
		{
			base.Plot(graphics, bounds, min, max);

			// draw rectangles of PnL breakeven zones for each position
			_positionManager.ViewModel.PlotAll();
		}

        protected override void OnTermination()
        {
#if DEBUG
            Print("NadexBoxer::OnTermination");
#endif
            if (_positionManager != null)
            {
                _positionManager.Close();
                _positionManager.ViewModel.SavePositionsToXml();
            }
        }
    }
}
