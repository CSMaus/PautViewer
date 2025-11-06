using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.Events;
using SciChart.Data.Model;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PAUTViewer.Views
{
    /// <summary>
    /// Interaction logic for CscanPAUserControl.xaml
    /// </summary>
    public partial class CscanPAUserControl : UserControl, INotifyPropertyChanged
    {
        #region Initial variables and fields definition
        // heatmap data (Z) shaped as [height=scans, width=samples]
        private UniformHeatmapDataSeries<double, double, double> _dataSeries;
        // sizing
        private int _scans;    // height
        private int _samples;  // width

        // world ranges
        private double _scanMin, _scanMax;   // from ScansLims
        private double _idxMin, _idxMax;    // from Xlims

        // initial steps (for line defaults)
        private double _scanStep = 1.0;
        private double _idxStep = 1.0;

        // events for external listeners
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedScan;
        public event LineMovedEventHandler LineMovedIndex;

        private int _channel;
        double _xStart, _xStep, _yStart, _yStep;

        #endregion

        public CscanPAUserControl()
        {
            InitializeComponent();
        }

        public void CreateScanPlotModel(
            int channel,
            int[] scansLims,
            float[] xlims,
            double maxVal,
            int scanCount,
            int sampleCount,
            double scanStep = 1.0,
            double indexStep = 1.0)
        {
            _channel = channel;
            _scans = scanCount;
            _samples = sampleCount;

            _scanMin = scansLims[0];
            _scanMax = scansLims[1];
            _idxMin = xlims[0];
            _idxMax = xlims[1];

            _scanStep = scanStep <= 0 ? 1.0 : scanStep;
            _idxStep = indexStep <= 0 ? 1.0 : indexStep;

            // Axes visible ranges
            // We keep the historical orientation: X axis = scans (top-down), so use negative step to flip.
            XAxis.VisibleRange = new DoubleRange(_scanMin, _scanMax);
            YAxis.VisibleRange = new DoubleRange(_idxMin, _idxMax);

            // Color range
            HeatmapSeries.ColorMap.Minimum = 0.0;
            HeatmapSeries.ColorMap.Maximum = Math.Max(1e-9, maxVal);

            // Empty initial Z array
            var z = new double[_scans, _samples];

            // Map: X = scans (use negative step to have X decreasing if needed), Y = index
            double xStart = _scanMax;
            double xStep = (_scans > 1) ? (_scanMin - _scanMax) / (_scans - 1) : -1.0;

            double yStart = _idxMin;
            double yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(z, xStart, xStep, yStart, yStep);
            HeatmapSeries.DataSeries = _dataSeries;

            // Init draggable lines
            ScanLine.X1 = _scanMin + _scanStep;
            IndexLine.Y1 = _idxMin + _idxStep;

            // X = scans (flipped)
            _xStart = _scanMax;
            _xStep = (_scans > 1) ? (_scanMin - _scanMax) / (_scans - 1) : -1.0;

            // Y = index
            _yStart = _idxMin;
            _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(
                new double[_scans, _samples], _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;
        }


        public void UpdateScanPlotModel(
           float[][][] currentData,
           int depthMin, int depthMax,
           float softGain = 1f)
        {
            if (currentData == null || currentData.Length == 0) return;

            int samples = currentData.Length;        // width
            int scans = currentData[0].Length;     // height
            int depths = currentData[0][0].Length;

            if (samples != _samples || scans != _scans)
            {
                _samples = samples;
                _scans = scans;

                _xStart = _scanMax;
                _xStep = (_scans > 1) ? (_scanMin - _scanMax) / (_scans - 1) : -1.0;
                _yStart = _idxMin;
                _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;
            }

            // clamp depth limits
            int d0 = Math.Max(0, Math.Min(depthMin, depths));
            int d1 = Math.Max(d0, Math.Min(depthMax, depths));

            var z = new double[_scans, _samples];

            // Fill Z[y=scan, x=sample] with max over depth
            Parallel.For(0, _samples, i =>
            {
                for (int s = 0; s < _scans; s++)
                {
                    float maxv = 0f;
                    var depthLine = currentData[i][s];
                    for (int d = d0; d < d1; d++)
                    {
                        float v = depthLine[d] * softGain;
                        if (v > maxv) maxv = v;
                    }
                    // row index s corresponds to scan line; store in [s, i]
                    z[s, i] = maxv;
                }
            });

            // Recreate series each update (fast & API-safe)
            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(z, _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;
        }

        // External programmatic updates of the lines (optional)
        public void UpdateScanLinePosition(double newScan)
        {
            ScanLine.X1 = newScan;
        }
        public void UpdateIndexLinePosition(double newIndex)
        {
            IndexLine.Y1 = newIndex;
        }

        private void ScanLine_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double x = ScanLine.X1 is double dx ? dx : Convert.ToDouble(ScanLine.X1);
            if (x < _scanMin) x = _scanMin;
            if (x > _scanMax) x = _scanMax;
            ScanLine.X1 = x;

            LineMovedScan?.Invoke(this, (float)x, _channel);
        }

        private void IndexLine_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double y = IndexLine.Y1 is double dy ? dy : Convert.ToDouble(IndexLine.Y1);
            if (y < _idxMin) y = _idxMin;
            if (y > _idxMax) y = _idxMax;
            IndexLine.Y1 = y;

            LineMovedIndex?.Invoke(this, (float)y, _channel);
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    }
}
