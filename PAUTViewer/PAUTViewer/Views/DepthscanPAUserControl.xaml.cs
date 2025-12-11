using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Events;
using SciChart.Data.Model;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Controls;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties;

namespace PAUTViewer.Views
{
    public partial class DepthscanPAUserControl : UserControl
    {
        private UniformHeatmapDataSeries<double, double, double> _dataSeries;
        public NumericAxis XAxisControl { get { return XAxis; } }
        public NumericAxis YAxisControl { get { return YAxis; } }
        private int _nx; // columns (distance/depth)
        private int _ny; // rows (beams/signals)

        private double[] _xlims = new double[2];
        private double[] _ylims = new double[2];

        private double _scanStep = 1.0;
        private double _depthStep = 1.0;

        private int _channel;
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedScanMax;
        public event LineMovedEventHandler LineMovedScanMin;

        public DepthscanPAUserControl()
        {
            InitializeComponent();
        }

        public void CreateScanPlotModel(int channel, float[] Ylims, int[] scansLims,
            int scanCount, int depthCount, double scanStep, float maxVal)
        {
            _channel = channel;

            _xlims[0] = scansLims[0]; _xlims[1] = scansLims[1];
            _ylims[0] = Ylims[0]; _ylims[1] = Ylims[1];


            _scanStep = Math.Abs(_xlims[1] - _xlims[0]) / scanCount;
            _depthStep = Math.Abs(_ylims[1] - _ylims[0]) / depthCount;

            XAxis.VisibleRange = new DoubleRange(_xlims[0], _xlims[1]);
            YAxis.VisibleRange = new DoubleRange(_ylims[0], _ylims[1]);

            HeatmapSeries.ColorMap.Minimum = 0.0;
            HeatmapSeries.ColorMap.Maximum = Math.Max(1e-9, maxVal);

            _dataSeries = null;
            HeatmapSeries.DataSeries = null;

            ScanLineMax.X1 = _xlims[0] + _scanStep;
        }

        public void UpdateScanPlotModel( float[][][] currentData, int idxStart, int idxEnd, bool projectAcrossIndex,
            int[] scansLims, float[] Ylims, double ampRelMin, double ampRelMax, float softGain = 1f)
        {
            if (currentData == null || currentData.Length == 0)
                return;

            int beams = currentData.Length;
            int scans = currentData[0].Length;
            int depthCount = currentData[0][0].Length;

            int i0, i1;
            
            i0 = idxStart < (beams-1) ? (idxStart >= 0 ? idxStart : 0) : beams - 1;
            i1 = idxEnd < (beams - 1) ? (idxEnd >= 0 ? idxEnd : 0) : beams - 1;

            var z = new double[depthCount, scans];

            float g = softGain == 0f ? 1f : softGain;


            if (projectAcrossIndex)
            {
                Parallel.For(0, scans, s =>
                {
                    for (int d = 0; d < depthCount; d++)
                    {
                        float maxv = 0f;

                        for (int b = i0; b < i1; b++)
                        {
                            float v = currentData[b][s][d] * g;
                            if (v > maxv) maxv = v;
                        }

                        z[d, s] = maxv;
                    }
                });
            }
            else
            {
                Parallel.For(0, scans, s =>
                {
                    for (int d = 0; d < depthCount; d++)
                    {
                        float v = currentData[i1][s][d] * g;
                        z[d, s] = v;
                    }
                });
            }

            _dataSeries = new UniformHeatmapDataSeries<double, double, double>(z, scansLims[0], _scanStep, Ylims[0], _depthStep);

            HeatmapSeries.DataSeries = _dataSeries;
            HeatmapSeries.ColorMap.Minimum = ampRelMin;
            HeatmapSeries.ColorMap.Maximum = ampRelMax;
        }

        public void UpdateScanLineMaxPosition(double newScan)
        {
            ScanLineMax.X1 = newScan;
        }
        public void UpdateScanLineMinPosition(double newScan)
        {
            ScanLineMin.X1 = newScan;
        }

        private void ScanLineMax_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double x = ScanLineMax.X1 is double dx ? dx : Convert.ToDouble(ScanLineMax.X1);
            if (x < _xlims[0]) x = _xlims[0];
            if (x > _xlims[1]) x = _xlims[1];
            ScanLineMax.X1 = x;

            LineMovedScanMax?.Invoke(this, (float)x, _channel);
        }
        private void ScanLineMin_OnDragDelta(object sender, AnnotationDragDeltaEventArgs e)
        {
            double x = ScanLineMin.X1 is double dx ? dx : Convert.ToDouble(ScanLineMin.X1);
            if (x < _xlims[0]) x = _xlims[0];
            if (x > _xlims[1]) x = _xlims[1];
            ScanLineMin.X1 = x;

            LineMovedScanMin?.Invoke(this, (float)x, _channel);
        }
    }
}
