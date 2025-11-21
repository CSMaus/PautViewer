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

        private int _scans;    // height (scan count)
        private int _samples;  // width (index/sample count)
        private int _depths;   // depth count (from data)

        private double _scanMin, _scanMax;   // ScansLims
        private double _idxMin, _idxMax;     // Xlims (index range)
        private double _depthMinW, _depthMaxW; // Ylims (depth range in world units)

        private double _xStart, _xStep;  // scans (flipped)
        private double _yStart, _yStep;  // index

        private double _scanStep = 1.0;
        private double _idxStep = 1.0;

        private double _ampMaxAbs = 1.0;

        private int _channel;

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
                int[] scansLims,
                float[] xlims,
                (double min, double max) depthWorldRange,
                double ampMaxAbs,
                int scanCount,
                int sampleCount,
                double scanStep = 1.0,
                double indexStep = 1.0)
        {
            _channel = channel;

            _scans = Math.Max(1, scanCount);
            _samples = Math.Max(1, sampleCount);

            _scanMin = scansLims[0]; _scanMax = scansLims[1];
            _idxMin = xlims[0]; _idxMax = xlims[1];
            _depthMinW = depthWorldRange.min; _depthMaxW = depthWorldRange.max;

            _scanStep = scanStep <= 0 ? 1.0 : scanStep;
            _idxStep = indexStep <= 0 ? 1.0 : indexStep;

            _ampMaxAbs = ampMaxAbs > 0 ? ampMaxAbs : 1.0;


            XAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_scanMin, _scanMax);
            YAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_idxMin, _idxMax);

            HeatmapSeries.ColorMap.Minimum = _depthMinW;
            HeatmapSeries.ColorMap.Maximum = _depthMaxW;

            _xStart = _scanMin;
            _xStep = (_scans > 1) ? (_scanMax - _scanMin) / (_scans - 1) : 1.0;
            _yStart = _idxMin;
            _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(
                new double[_scans, _samples], _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;

            XAxis.VisibleRange = new DoubleRange(_scanMin, _scanMax);
            YAxis.VisibleRange = new DoubleRange(_idxMin, _idxMax);
            ScanLine.X1 = _scanMin + _scanStep;
            IndexLine.Y1 = _idxMin + _idxStep;
        }



        // currentData[d][s][b]
        public void UpdateScanPlotModel(
    float[][][] currentData,
    int depthMinIdx, int depthMaxIdx,
    double ampRelMin, double ampRelMax,
    double _ = 1.0, float softGain = 1f)
        {
            if (currentData == null || currentData.Length == 0) return;

            int beams = currentData.Length;           // Y axis (index)
            int scans = currentData[0].Length;        // X axis (scans)
            int depth = currentData[0][0].Length;     // projection dim

            if (beams != _samples || scans != _scans)
            {
                _samples = beams;
                _scans = scans;

                _xStart = _scanMin;
                _xStep = (_scans > 1) ? (_scanMax - _scanMin) / (_scans - 1) : 1.0;

                _yStart = _idxMin;
                _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;
            }

            int d0 = (depthMinIdx < 0) ? 0 : Math.Clamp(depthMinIdx, 0, depth - 1);
            int d1 = (depthMaxIdx < 0) ? depth : Math.Clamp(depthMaxIdx, d0 + 1, depth);

            double gateAbs = Math.Clamp(ampRelMin, 0.0, 1.0) * Math.Max(_ampMaxAbs, 1e-9);

            double depthW0 = _depthMinW;
            double depthW1 = _depthMaxW;
            double dyW = (depth > 1) ? (depthW1 - depthW0) / (depth - 1) : 0.0;

            var z = new double[_samples, _scans];   // z[beam, scan] = depthWorldOfMax
            float g = (softGain == 0f) ? 1f : softGain;

            System.Threading.Tasks.Parallel.For(0, scans, s =>
            {
                for (int b = 0; b < beams; b++)
                {
                    int argMax = d0;
                    float maxVal = float.NegativeInfinity;

                    var depthLine = currentData[b][s]; // length = depth
                    for (int d = d0; d < d1; d++)
                    {
                        float v = depthLine[d] * g;
                        if (v > maxVal) { maxVal = v; argMax = d; }
                    }

                    z[b, s] = (maxVal >= gateAbs) ? (depthW0 + argMax * dyW)
                                                  : depthW0;
                }
            });

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(z, _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;


            // XAxis.VisibleRange = new DoubleRange(_scanMin, _scanMax);
            // YAxis.VisibleRange = new DoubleRange(_idxMin, _idxMax);
        }




        public void UpdateScanLinePosition(double newScan) => ScanLine.X1 = newScan;
        public void UpdateIndexLinePosition(double newIndex) => IndexLine.Y1 = newIndex;

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
