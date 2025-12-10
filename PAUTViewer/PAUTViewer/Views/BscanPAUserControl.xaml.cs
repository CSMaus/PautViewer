using PAUTViewer.ViewModels;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Data.Model;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PAUTViewer.Views
{
    public partial class BscanPAUserControl : UserControl
    {
        private UniformHeatmapDataSeries<double, double, double> _dataSeries;
        private int _nx; // columns (distance/depth)
        private int _ny; // rows (beams/signals)

        private double[] _xlims = new double[2];
        private double[] _ylims = new double[2];

        private int _channel;
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedIndex;

        public BscanPAUserControl()
        {
            InitializeComponent();
        }

        public void CreateScanPlotModel(int channel, float[] Ylims, float[] Scanlims, float maxVal)
        {
            _channel = channel;

            _xlims[0] = Scanlims[0]; _xlims[1] = Scanlims[1];
            _ylims[0] = Ylims[0]; _ylims[1] = Ylims[1];

            XAxis.VisibleRange = new DoubleRange(_xlims[0], _xlims[1]);
            YAxis.VisibleRange = new DoubleRange(_ylims[0], _ylims[1]);

            HeatmapSeries.ColorMap.Minimum = 0.0;
            HeatmapSeries.ColorMap.Maximum = Math.Max(1e-9, maxVal);

            _dataSeries = null;
            HeatmapSeries.DataSeries = null;

            if (_xlims[1] > _xlims[0])
                IndexLineMax.X1 = (_xlims[0] + _xlims[1]) * 0.5;
            else
                IndexLineMax.X1 = _xlims[0];

        }

        public void UpdateScanPlotModel(float[][][] currentData, int scanStart, int scanEnd, bool projectAcrossScan, 
            int scan, float[] Xlims, float[] Ylims, float softGain)
        {
            if (currentData == null || currentData.Length == 0) return;
            
            int beams = currentData.Length;          // rows (Y)
            int scans = currentData[0].Length;
            int depthCount = currentData[0][0].Length;    // cols (X)

            int scanIdx = scan < scans ? scan : scans - 1;
            if (scanIdx < 0) scanIdx = 0;

            _ny = beams;
            _nx = depthCount;

            _xlims[0] = Xlims[0]; _xlims[1] = Xlims[1];
            _ylims[0] = Ylims[0]; _ylims[1] = Ylims[1];
            float g = (softGain == 0f) ? 1.0f : softGain;

            int s0, s1;
            s0 = scanStart < (scans - 1) ? (scanStart >= 0 ? scanStart : 0) : scans - 1;
            s1 = scanEnd < (scans - 1) ? (scanEnd >= 0 ? scanEnd : 0) : scans - 1;

            var data = new double[_nx, _ny];

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
                        data[j, i] = maxv;
                    }
                });
            }
            else
            {
                Parallel.For(0, _ny, i =>
                {
                    var row = currentData[i][scanIdx];
                    for (int j = 0; j < _nx; j++)
                        data[j, i] = row[j] * g;
                });
            }

            double xStart = _xlims[0];
            double xStep = (_ny > 1) ? (_xlims[1] - _xlims[0]) / (_ny - 1) : 1.0;
            double yStart = _ylims[0];
            double yStep = (_nx > 1) ? (_ylims[1] - _ylims[0]) / (_nx - 1) : 1.0;

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(data, xStart, xStep, yStart, yStep);
            HeatmapSeries.DataSeries = _dataSeries;

            // XAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_xlims[0], _xlims[1]);
            // YAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(_ylims[0], _ylims[1]);
        }

        public void UpdateIndexLinePosition(double newIndex)
        {
            IndexLineMax.X1 = newIndex;
        }

        private void IndexLine_OnDragDelta(object sender, SciChart.Charting.Visuals.Events.AnnotationDragDeltaEventArgs e)
        {
            double x = IndexLineMax.X1 is double dx ? dx : Convert.ToDouble(IndexLineMax.X1);

            if (x < _xlims[0]) x = _xlims[0];
            if (x > _xlims[1]) x = _xlims[1];

            IndexLineMax.X1 = x;

            LineMovedIndex?.Invoke(this, (float)x, _channel);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
