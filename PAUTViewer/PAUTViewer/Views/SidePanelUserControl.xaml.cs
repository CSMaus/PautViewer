using PAUTViewer.Models;
using PAUTViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class SidePanelUserControl : UserControl, INotifyPropertyChanged
    {
        public SidePanelUserControl()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new PlotPAViewModel(new DataLoader());
            }
        }

        public SidePanelUserControl(PlotPAViewModel sharedPlotPAViewModel) : this()
        {
            DataContext = sharedPlotPAViewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
