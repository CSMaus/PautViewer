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
    /// Interaction logic for DscanPAUserControl.xaml
    /// </summary>
    public partial class DscanPAUserControl : UserControl, INotifyPropertyChanged
    {
        #region Fields

        private UniformHeatmapDataSeries<double, double, double> _dataSeries;

        // Sizes
        private int _scans;    // height (scan count)
        private int _samples;  // width (index/sample count)
        private int _depths;   // depth count (from data)

        // World ranges for axes
        private double _scanMin, _scanMax;   // ScansLims
        private double _idxMin, _idxMax;     // Xlims (index range)
        private double _depthMinW, _depthMaxW; // Ylims (depth range in world units)

        // DataSeries mapping (X/Y in world)
        private double _xStart, _xStep;  // scans (flipped)
        private double _yStart, _yStep;  // index

        // Defaults for draggable lines
        private double _scanStep = 1.0;
        private double _idxStep = 1.0;

        // Amplitude scale (absolute max)
        private double _ampMaxAbs = 1.0;

        // Channel for callbacks
        private int _channel;

        // Events out
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedScan;
        public event LineMovedEventHandler LineMovedIndex;

        #endregion

        public DscanPAUserControl()
        {
            InitializeComponent();
        }


        public void CreateScanPlotModel(
    int channel,
    int[] scansLims,           // X axis (scans)
    float[] xlims,             // Y axis (index)
    (double min, double max) depthWorldRange, // Z colormap (DEPTH in world units)
    double ampMaxAbs,
    int scanCount,
    int sampleCount,
    double scanStep = 1.0,
    double indexStep = 1.0)
        {
            _channel = channel;

            _scans = Math.Max(1, scanCount);   // height
            _samples = Math.max(1, sampleCount); // width

            _scanMin = scansLims[0];
            _scanMax = scansLims[1];
            _idxMin = xlims[0];
            _idxMax = xlims[1];

            _scanStep = scanStep <= 0 ? 1.0 : scanStep;
            _idxStep = indexStep <= 0 ? 1.0 : indexStep;

            _ampMaxAbs = ampMaxAbs > 0 ? ampMaxAbs : 1.0;

            // Z (depth) color range ONLY
            _depthMinW = depthWorldRange.min;
            _depthMaxW = depthWorldRange.max;

            // Axes (same as C-scan)
            XAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_scanMin, _scanMax);
            YAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_idxMin, _idxMax);

            // Colormap = depth world
            HeatmapSeries.ColorMap.Minimum = _depthMinW;
            HeatmapSeries.ColorMap.Maximum = _depthMaxW;

            // Mapping: X = scans (flipped), Y = index
            _xStart = _scanMax;
            _xStep = (_scans > 1) ? (_scanMin - _scanMax) / (_scans - 1) : -1.0;
            _yStart = _idxMin;
            _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(
                new double[_scans, _samples], _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;

            // Draggable lines (same semantics as C-scan)
            ScanLine.X1 = _scanMin + _scanStep;
            IndexLine.Y1 = _idxMin + _idxStep;
        }



        public void UpdateScanPlotModel(
    float[][][] currentData,
    int depthMinIdx,
    int depthMaxIdx,
    double ampRelMin,
    double ampRelMax = 1.0, // reserved
    float softGain = 1f)
        {
            if (currentData == null || currentData.Length == 0) return;

            int samples = currentData.Length;        // width
            int scans = currentData[0].Length;     // height
            int depths = currentData[0][0].Length;  // depth count
            _depths = depths;

            if (samples != _samples || scans != _scans)
            {
                _samples = samples;
                _scans = scans;
                _xStart = _scanMax;
                _xStep = (_scans > 1) ? (_scanMin - _scanMax) / (_scans - 1) : -1.0;
                _yStart = _idxMin;
                _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;
            }

            int d0 = Math.Max(0, Math.Min(depthMinIdx, depths));
            int d1 = Math.Max(d0, Math.Min(depthMaxIdx, depths));

            double thrAbs = Math.Clamp(ampRelMin, 0.0, 1.0) * _ampMaxAbs;
            double dyW = (_depths > 1) ? (_depthMaxW - _depthMinW) / (_depths - 1) : 0.0;

            var z = new double[_scans, _samples];

            System.Threading.Tasks.Parallel.For(0, _samples, i =>
            {
                for (int s = 0; s < _scans; s++)
                {
                    var depthLine = currentData[i][s];
                    double outDepth = double.NaN;

                    for (int d = d0; d < d1; d++)
                    {
                        if (depthLine[d] * softGain >= thrAbs)
                        {
                            outDepth = _depthMinW + d * dyW;
                            break; // first crossing
                        }
                    }
                    z[s, i] = outDepth;
                }
            });

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(z, _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;
        }



        public void UpdateScanLinePosition(double newScan) => ScanLine.X1 = newScan;
        public void UpdateIndexLinePosition(double newIndex) => IndexLine.Y1 = newIndex;

        // Drag handlers
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
