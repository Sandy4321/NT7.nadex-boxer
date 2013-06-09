using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NadexBoxer
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>    
    public partial class PositionManager : Window
    {
        private PositionManagerViewModel _positionManagerViewModel;
        public PositionManagerViewModel ViewModel { get { return _positionManagerViewModel; } }

        public PositionManager()
        {
            InitializeComponent();
            _positionManagerViewModel = new PositionManagerViewModel(this);
            DataContext = ViewModel;

           Uri iconUri = new Uri("pack://application:,,,/NadexBoxer;component/logo.png", UriKind.RelativeOrAbsolute);
           this.Icon = BitmapFrame.Create(iconUri);
        }

		private void OnAddPosition(object sender, RoutedEventArgs e)
		{
			ViewModel.OnAddPositionCommand(this, new EventArgs());
		}

		private void OnRemovePosition(object sender, RoutedEventArgs e)
		{
			ViewModel.OnRemovePositionCommand(this, new EventArgs());
		}

		private void OnContractTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.OnContractTypeSelectionChanged();
		}
    }
}
