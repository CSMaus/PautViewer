using PAUTViewer.ViewModels;
using System;
using System.Collections.Generic;
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

namespace PAUTViewer.Views
{
    /// <summary>
    /// Interaction logic for SidePanelUserControl.xaml
    /// </summary>
    public partial class SidePanelUserControl : UserControl
    {
        public SidePanelUserControl(PlotPAViewModel sharedPlotPAViewModel)
        {
            InitializeComponent();
            DataContext = sharedPlotPAViewModel;
        }

        private void Channels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
