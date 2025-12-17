using PAUTViewer.ViewModels;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PAUTViewer.Views
{
    public partial class BscanPAUserControl : UserControl, INotifyPropertyChanged
    {
        private UniformHeatmapDataSeries<double, double, double> _dataSeries;
        public NumericAxis XAxisControl { get { return XAxis; } }
        public NumericAxis YAxisControl { get { return YAxis; } }
        private int _nindex; // columns (distance/depth)
        private int _ndepth; // rows (beams/signals)

        private double[] _xlims = new double[2];
        private double[] _ylims = new double[2];

        private int _channel;
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedIndexMax;
        public event LineMovedEventHandler LineMovedIndexMin;

        public BscanPAUserControl()
        {
            InitializeComponent();
        }

        public void CreateScanPlotModel(int channel, float[] Ylims, float[] Xlims, float maxVal)
        {
            _channel = channel;

            _ylims[0] = Xlims[0]; _ylims[1] = Xlims[1];  // this is index axis
            _xlims[0] = Ylims[0]; _xlims[1] = Ylims[1]; // this is depth coordinate

            XAxis.VisibleRange = new DoubleRange(_xlims[0], _xlims[1]);
            YAxis.VisibleRange = new DoubleRange(_ylims[0], _ylims[1]);

            HeatmapSeries.ColorMap.Minimum = 0.0;
            HeatmapSeries.ColorMap.Maximum = Math.Max(1e-9, maxVal);

            _dataSeries = null;
            HeatmapSeries.DataSeries = null;

            if (_ylims[1] > _ylims[0])
                IndexLineMax.Y1 = (_ylims[0] + _ylims[1]) * 0.5;
            else
                IndexLineMax.Y1 = _ylims[0];

            IndexLineMax.Y1 = _ylims[0] + (_ylims[1] - _ylims[0]) / 4;
        }

        public void UpdateScanPlotModel(float[][][] currentData, int scanStart, int scanEnd, bool projectAcrossScan,
            float[] Indexlims, float[] Depthlims, float softGain)
        {
            if (currentData == null || currentData.Length == 0) return;
            
            int beams = currentData.Length;          // rows (Y)
            int scans = currentData[0].Length;
            int depthCount = currentData[0][0].Length;    // cols (X)

            // int scanIdx = scan < scans ? scan : scans - 1;
            // if (scanIdx < 0) scanIdx = 0;

            _nindex = beams;
            _ndepth = depthCount;

            _ylims[0] = Indexlims[0]; _ylims[1] = Indexlims[1]; // index axis - should be vertical
            _xlims[0] = Depthlims[0]; _xlims[1] = Depthlims[1];  // depth axis - should be horizontal
            float g = (softGain == 0f) ? 1.0f : softGain;

            int s0, s1;
            s0 = scanStart < (scans - 1) ? (scanStart >= 0 ? scanStart : 0) : scans - 1;
            s1 = scanEnd < (scans - 1) ? (scanEnd >= 0 ? scanEnd : 0) : scans - 1;

            var data = new double[_nindex, _ndepth];

            if (projectAcrossScan)
            {
                Parallel.For(0, beams, i =>
                {
                    for (int j = 0; j < depthCount; j++)
                    {
                        float maxv = 0f;
                        for (int s = s0; s < s1; s++)
                        {
                            float v = currentData[i][s][j] * g;
                            if (v > maxv) maxv = v;
                        }
                        data[i, j] = maxv;
                    }
                });
            }
            else
            {
                Parallel.For(0, _nindex, i =>
                {
                    var row = currentData[i][s1];
                    for (int j = 0; j < _ndepth; j++)
                        data[i, j] = row[j] * g;
                });
            }

            double xStart = _xlims[0];
            double xStep = (_ndepth > 1) ? (_xlims[1] - _xlims[0]) / (_ndepth - 1) : 1.0;
            double yStart = _ylims[0];
            double yStep = (_nindex > 1) ? (_ylims[1] - _ylims[0]) / (_nindex - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(data, xStart, xStep, yStart, yStep);
            HeatmapSeries.DataSeries = _dataSeries;

            // XAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_xlims[0], _xlims[1]);
            // YAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_ylims[0], _ylims[1]);
        }

        public void UpdateIndexLineMaxPosition(double newIndex)
        {
            IndexLineMax.Y1 = newIndex;
        }

        public void UpdateIndexLineMinPosition(double newIndex)
        {
            IndexLineMin.Y1 = newIndex;
        }

        private void IndexLineMax_OnDragDelta(object sender, SciChart.Charting.Visuals.Events.AnnotationDragDeltaEventArgs e)
        {
            double y = IndexLineMax.Y1 is double dy ? dy : Convert.ToDouble(IndexLineMax.Y1);

            if (y < _ylims[0]) y = _ylims[0];
            if (y > _ylims[1]) y = _ylims[1];

            IndexLineMax.Y1 = y;

            LineMovedIndexMax?.Invoke(this, (float)y, _channel);
        }

        private void IndexLineMin_OnDragDelta(object sender, SciChart.Charting.Visuals.Events.AnnotationDragDeltaEventArgs e)
        {
            double y = IndexLineMin.Y1 is double dy ? dy : Convert.ToDouble(IndexLineMin.Y1);

            if (y < _ylims[0]) y = _ylims[0];
            if (y > _ylims[1]) y = _ylims[1];

            IndexLineMin.Y1 = y;

            LineMovedIndexMin?.Invoke(this, (float)y, _channel);
        }

        #region Plot color maps and view setup

        public void SetColorMap(string cmapName)
        {
            if (string.IsNullOrWhiteSpace(cmapName)) return;

            HeatmapSeries.ColorMap = cmapName switch
            {
                "Jet" => CreateJetPalette(),
                "Gray" => CreateGrayPalette(),
                _ => HeatmapSeries.ColorMap
            };

            Surface.InvalidateElement();
        }

        private HeatmapColorPalette CreateJetPalette()
        {
            // same as your XAML Jet palette
            return new HeatmapColorPalette
            {
                Minimum = HeatmapSeries.ColorMap.Minimum,
                Maximum = HeatmapSeries.ColorMap.Maximum,
                GradientStops = new ObservableCollection<GradientStop>
        {
            new GradientStop((Color)ColorConverter.ConvertFromString("#00007F"), 0.00),
            new GradientStop((Color)ColorConverter.ConvertFromString("#0000FF"), 0.17),
            new GradientStop((Color)ColorConverter.ConvertFromString("#00FFFF"), 0.33),
            new GradientStop((Color)ColorConverter.ConvertFromString("#00FF00"), 0.50),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FFFF00"), 0.67),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FF7F00"), 0.83),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FF0000"), 1.00),
        }
            };
        }

        private HeatmapColorPalette CreateGrayPalette()
        {
            return new HeatmapColorPalette
            {
                Minimum = HeatmapSeries.ColorMap.Minimum,
                Maximum = HeatmapSeries.ColorMap.Maximum,
                GradientStops = new ObservableCollection<GradientStop>
        {
            new GradientStop(Colors.Black, 0.0),
            new GradientStop(Colors.White, 1.0),
        }
            };
        }

        #endregion
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
