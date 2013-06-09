using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UITester
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            NadexBoxer.PositionManager positionBrowser = new NadexBoxer.PositionManager();
            //positionBrowser.ViewModel.SavePositionsToXml();
            positionBrowser.ShowDialog();
        }
    }
}
