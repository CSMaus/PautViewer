using PAUTViewer.Models;
using PAUTViewer.ViewModels;
using PAUTViewer.Views;
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

namespace PAUTViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private DataLoader loadedData;
        private PlotPAView plotPAView;
        private SidePanelUserControl sidePanel;
        public static MenuUserControl _menuUserControl;
        //private FlawTableUserControl flawTableUserControl;

        private IDisposable _apiServer;
        public static PlotPAViewModel SharedPlotPAViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            loadedData = new DataLoader();
            SharedPlotPAViewModel = new PlotPAViewModel(loadedData);

            _menuUserControl = new MenuUserControl(this, loadedData, SharedPlotPAViewModel);
            plotPAView = new PlotPAView(SharedPlotPAViewModel);
            sidePanel = new SidePanelUserControl(SharedPlotPAViewModel);

            //ExplorerFrame.Navigate(_menuUserControl);
            //PAFrame.Navigate(plotPAView);
            //SidePanelFrame.Navigate(sidePanel);

            ExplorerFrame.Content = _menuUserControl;
            PAFrame.Content = plotPAView;
            SidePanelFrame.Content = sidePanel;
        }

        public void UpdatePAPlotDataContext(PlotPAViewModel viewModel)
        {
            plotPAView.DataContext = null;
            plotPAView.DataContext = viewModel;
            sidePanel.DataContext = null;
            sidePanel.DataContext = viewModel;
        }

    }
}