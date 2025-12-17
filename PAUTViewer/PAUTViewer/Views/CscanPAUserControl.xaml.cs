using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Events;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace PAUTViewer.Views
{
    public partial class CscanPAUserControl : UserControl, INotifyPropertyChanged
    {
        #region Initial variables and fields definition
        // heatmap data (Z) shaped as [height=scans, width=samples]
        private UniformHeatmapDataSeries<double, double, double> _dataSeries;

        public NumericAxis XAxisControl { get { return XAxis; } }
        public NumericAxis YAxisControl { get { return YAxis; } }

        private int _scans;    // width
        private int _samples;  // height
        private double _scanMin, _scanMax;   // from ScansLims
        private double _idxMin, _idxMax;    // from Xlims
        private double _scanStep = 1.0;
        private double _idxStep = 1.0;

        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedScanMin;
        public event LineMovedEventHandler LineMovedScanMax;
        public event LineMovedEventHandler LineMovedIndexMax;
        public event LineMovedEventHandler LineMovedIndexMin;

        private int _channel;
        double _xStart, _xStep, _yStart, _yStep;
        double maxAmp;

        // for SNR analysis, so we could easily get currently ploted data as 2d array
        public double[,] CscanData { get; private set; } = new double[0, 0];

        public Dictionary<string, (int startIdx, int endIdx, float yMinWorld, float yMaxWorld)> Gates { get; set; }
                 = new Dictionary<string, (int, int, float, float)>();
        public string? SelectedGateKey { get; set; }
        public void GatesPropertyChanged() => OnPropertyChanged(nameof(Gates));

        #endregion

        public CscanPAUserControl()
        {
            InitializeComponent();
        }

        public void CreateScanPlotModel( int channel, int[] scansLims, float[] xlims, double maxVal,
            int scanCount, int sampleCount, double scanStep, double indexStep = 1.0)
        {
            _channel = channel;
            _scans = scanCount;
            _samples = sampleCount;

            _scanMin = scansLims[0];
            _scanMax = scansLims[1];
            _idxMin = xlims[0];
            _idxMax = xlims[1];

            _idxStep = Math.Abs(_idxMax - _idxMin) / sampleCount;

            XAxis.VisibleRange = new DoubleRange(_scanMin, _scanMax);
            YAxis.VisibleRange = new DoubleRange(_idxMin, _idxMax);

            HeatmapSeries.ColorMap.Minimum = 0.0;
            HeatmapSeries.ColorMap.Maximum = Math.Max(1e-9, maxVal);

            var z = new double[_scans, _samples];

            ScanLineMax.X1 = _scanMax - _scanStep;
            IndexLineMax.Y1 = _idxMax - _idxStep;

            ScanLineMin.X1 = _scanMin + (_scanMax - _scanMin)/4 + _scanStep;
            IndexLineMin.Y1 = _idxMin + (_idxMax - _idxMin) / 4 + _idxStep;

            _xStart = _scanMin;
            _xStep = (_scans > 1) ? (_scanMax - _scanMin) / (_scans - 1) : -1.0;
            _yStart = _idxMin;
            _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(
                new double[_scans, _samples], _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;
        }


        // currentData[d][s][b]
        public void UpdateScanPlotModel(float[][][] currentData, int depthMin, int depthMax, float softGain = 1f)
        {
            if (currentData == null || currentData.Length == 0) return;

            int beams = currentData.Length;          // INDEX (Y-axis)
            int scans = currentData[0].Length;       // SCANS (X-axis)
            int depth = currentData[0][0].Length;    // DEPTH (projection dimension)

            if (beams != _samples || scans != _scans)
            {
                _samples = beams;
                _scans = scans;

                _xStart = _scanMin;
                _xStep = (_scans > 1) ? (_scanMax - _scanMin) / (_scans - 1) : 1.0;

                _yStart = _idxMin;
                _yStep = (_samples > 1) ? (_idxMax - _idxMin) / (_samples - 1) : 1.0;
            }

            int d0 = (depthMin < 0) ? 0 : Math.Clamp(depthMin, 0, depth - 1);
            //int d1 = (depthMax < 0) ? depth : Math.Clamp(depthMax, d0 + 1, depth);
            int d1 = depthMax < 0 ? depth : Math.Min(Math.Max(depthMax, 0), depth);

            if (d1 <= d0) d1 = Math.Min(d0 + 1, depth);

            var z = new double[_samples, _scans];   // z[index, scan]

            float g = (softGain == 0f) ? 1f : softGain;

            Parallel.For(0, scans, s =>
            {
                for (int b = 0; b < beams; b++)
                {
                    float maxv = 0f;
                    for (int d = d0; d < d1; d++)
                    {
                        float v = currentData[b][s][d] * g;
                        if (v > maxv) maxv = v;
                    }
                    z[b, s] = maxv;
                }
            });

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(
                z, _xStart, _xStep, _yStart, _yStep);
            HeatmapSeries.DataSeries = _dataSeries;

            CscanData = z;
            // XAxis.VisibleRange = new DoubleRange(_scanMin, _scanMax);
            // YAxis.VisibleRange = new DoubleRange(_idxMin, _idxMax);

        }


        #region Line Annotations manipulations
        public void UpdateScanLineMaxPosition(double newScan)
        {
            ScanLineMax.X1 = newScan;
        }
        public void UpdateIndexLineMaxPosition(double newIndex)
        {
            IndexLineMax.Y1 = newIndex;
        }

        public void UpdateScanLineMinPosition(double newScan)
        {
            ScanLineMin.X1 = newScan;
        }
        public void UpdateIndexLineMinPosition(double newIndex)
        {
            IndexLineMin.Y1 = newIndex;
        }

        private void ScanLineMax_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double x = ScanLineMax.X1 is double dx ? dx : Convert.ToDouble(ScanLineMax.X1);
            if (x < _scanMin) x = _scanMin;
            if (x > _scanMax) x = _scanMax;
            ScanLineMax.X1 = x;

            LineMovedScanMax?.Invoke(this, (float)x, _channel);
        }

        private void ScanLineMin_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double x = ScanLineMin.X1 is double dx ? dx : Convert.ToDouble(ScanLineMin.X1);
            if (x < _scanMin) x = _scanMin;
            if (x > _scanMax) x = _scanMax;
            ScanLineMin.X1 = x;

            LineMovedScanMin?.Invoke(this, (float)x, _channel);
        }

        private void IndexLineMax_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double y = IndexLineMax.Y1 is double dy ? dy : Convert.ToDouble(IndexLineMax.Y1);
            if (y < _idxMin) y = _idxMin;
            if (y > _idxMax) y = _idxMax;
            IndexLineMax.Y1 = y;

            LineMovedIndexMax?.Invoke(this, (float)y, _channel);
        }

        private void IndexLineMin_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double y = IndexLineMin.Y1 is double dy ? dy : Convert.ToDouble(IndexLineMin.Y1);
            if (y < _idxMin) y = _idxMin;
            if (y > _idxMax) y = _idxMax;
            IndexLineMin.Y1 = y;

            LineMovedIndexMin?.Invoke(this, (float)y, _channel);
        }
        #endregion


        #region SNR analysis things
        public BoxAnnotation? _snrBox { get; private set; }

        public bool IsSNRMarkerAdded() => _snrBox != null;

        public void AddRectAnnotation()
        {
            if (_snrBox != null) return;

            _snrBox = new BoxAnnotation
            {
                CoordinateMode = AnnotationCoordinateMode.Absolute,

                // X1/X2/Y1/Y2 in WORLD coordinates (same as your line annotations)
                X1 = _scanMin + (_scanMax - _scanMin) * 0.25,
                X2 = _scanMin + (_scanMax - _scanMin) * 0.75,
                Y1 = _idxMin + (_idxMax - _idxMin) * 0.25,
                Y2 = _idxMin + (_idxMax - _idxMin) * 0.75,
                BorderBrush = Brushes.Magenta,
                BorderThickness = new System.Windows.Thickness(3),
                IsEditable = true,
            };

            Surface.Annotations.Add(_snrBox);
            Surface.InvalidateElement();
        }

        public void RemoveRectAnnotation()
        {
            if (_snrBox == null) return;

            Surface.Annotations.Remove(_snrBox);
            _snrBox = null;
            Surface.InvalidateElement();
        }


        private FastUniformHeatmapRenderableSeries? _maskSeries;
        public void RemoveMaskSeries()
        {
            if (_maskSeries == null) return;

            Surface.RenderableSeries.Remove(_maskSeries);
            _maskSeries = null;
            Surface.InvalidateElement();
        }

        public void SetMask(double[,] markedData)
        {
            if (_maskSeries == null)
            {
                _maskSeries = new FastUniformHeatmapRenderableSeries
                {
                    Opacity = 0.85,
                    ColorMap = new HeatmapColorPalette
                    {
                        Minimum = 0,
                        Maximum = 1,
                        GradientStops = new ObservableCollection<GradientStop>
                        {
                            new GradientStop(Color.FromArgb(0,   0,   0,   0), 0.0),
                            new GradientStop(Color.FromArgb(200, 255, 255,   0), 1.0),
                        }
                    }
                };

                Surface.RenderableSeries.Add(_maskSeries);
            }

            var ds = new UniformHeatmapDataSeries<double, double, double>(
                markedData, _xStart, _xStep, _yStart, _yStep);

            _maskSeries.DataSeries = ds;
            Surface.InvalidateElement();
        }


        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    }
}
