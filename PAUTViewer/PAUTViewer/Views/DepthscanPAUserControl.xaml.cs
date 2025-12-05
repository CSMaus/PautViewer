using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
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
    public partial class DepthscanPAUserControl : UserControl
    {
        #region Initial variables and fields definition
        // heatmap data (Z) shaped as [height=scans, width=samples]
        private UniformHeatmapDataSeries<double, double, double> _dataSeries;
        // sizing
        private int _scans;    // width
        private int _depth;  // height

        // world ranges
        private double _scanMin, _scanMax;   // from ScansLims
        private double _depthMin, _depthMax;    // from Xlims

        // initial steps (for line defaults)
        private double _scanStep = 1.0;
        private double _depthStep = 1.0;

        // events for external listeners
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedScan;
        public event LineMovedEventHandler LineMovedScanMin;

        private int _channel;
        double _xStart, _xStep, _yStart, _yStep;
        double maxAmp;

        #endregion

        public DepthscanPAUserControl()
        {
            InitializeComponent();
        }
    }
}
