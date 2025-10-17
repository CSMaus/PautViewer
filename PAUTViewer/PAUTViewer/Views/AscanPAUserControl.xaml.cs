using OlympusNDT.Storage.NET;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.Axes;
using SciChart.Data.Model;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Controls;

namespace PAUTViewer.Views
{
    /// <summary>
    /// Interaction logic for AscanPAUserControl.xaml
    /// </summary>
    public partial class AscanPAUserControl : UserControl, INotifyPropertyChanged
    {
        #region Fields definition

        public XyDataSeries<double, double> LineDataSeries { get; } = new();
        public XyDataSeries<double, double> LineDataSeriesSoft { get; } = new();

        // events (compatible with your old delegates)
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedMin;
        public event LineMovedEventHandler LineMovedMax;

        // percent axis label format (bound in XAML)
        public string PercentFormat { get; set; } = "{0:0}%";

        private double _ampMin, _ampMax;
        private int _channel;

        #endregion




        public AscanPAUserControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Build axes, series, and lines.
        /// mpsLims: [xMin,xMax]; ampsLim: [yMin,yMax]
        /// </summary>
        public void CreateAscanPlotModel(float[] mpsLims, float[] ampsLim, int ichan)
        {
            _channel = ichan;
            _ampMin = ampsLim[0];
            _ampMax = ampsLim[1];

            // Axes visible ranges
            XAxis.VisibleRange = new DoubleRange(mpsLims[0], mpsLims[1]);
            YAxisLeft.VisibleRange = new DoubleRange(ampsLim[0], ampsLim[1]);

            // Right percentage axis: 0..100, labels only (no grid)
            YAxisRightPct.VisibleRange = new DoubleRange(0, 100);

            // Place draggable lines inside initial range
            double xSpan = mpsLims[1] - mpsLims[0];
            VLineMin.X1 = mpsLims[0] + 0.20 * xSpan;
            VLineMax.X1 = mpsLims[0] + 0.80 * xSpan;

            double ySpan = ampsLim[1] - ampsLim[0];
            HLine1.Y1 = ampsLim[0] + 0.30 * ySpan;
            HLine2.Y1 = ampsLim[0] + 0.70 * ySpan;

            // Hook built-in drag events (update labels + raise your events)
            VLineMin.DragDelta += (_, e) =>
            {
                // clamp to X axis range
                var rx = (DoubleRange)XAxis.VisibleRange;
                VLineMin.X1 = Math.Min(rx.Max, Math.Max(rx.Min, VLineMin.X1));
                VMinLabel.Text = $"{VLineMin.X1:0.00} mm";
                LineMovedMin?.Invoke(this, (float)VLineMin.X1, _channel);
            };
            VLineMax.DragDelta += (_, e) =>
            {
                var r = XAxis.VisibleRange;
                VLineMax.X1 = Math.Min(r.MaxAsDouble, Math.Max(r.MinAsDouble, VLineMax.X1));
                VMaxLabel.Text = $"{VLineMax.X1:0.00} mm";
                LineMovedMax?.Invoke(this, (float)VLineMax.X1, _channel);
            };

            // Horizontal labels show value + percent of amplitude range
            void UpdateHLabel(HorizontalLineAnnotation h, AnnotationLabel label)
            {
                var pct = (_ampMax > _ampMin) ? (h.Y1 - _ampMin) / (_ampMax - _ampMin) * 100.0 : 0.0;
                label.Text = $"{h.Y1:0.00}  ({pct:0.#}%)";
            }

            HLine1.DragDelta += (_, __) =>
            {
                var r = YAxisLeft.VisibleRange;
                HLine1.Y1 = Math.Min(r.MaxAsDouble, Math.Max(r.MinAsDouble, HLine1.Y1));
                UpdateHLabel(HLine1, H1Label);
            };
            HLine2.DragDelta += (_, __) =>
            {
                var r = YAxisLeft.VisibleRange;
                HLine2.Y1 = Math.Min(r.MaxAsDouble, Math.Max(r.MinAsDouble, HLine2.Y1));
                UpdateHLabel(HLine2, H2Label);
            };
            // initialize labels once
            UpdateHLabel(HLine1, H1Label);
            UpdateHLabel(HLine2, H2Label);
        }

        /// <summary>
        /// Update A-scan line from your 3D array (angles x scans x length).
        /// </summary>
        public void UpdateAscanPlotModel(float[][][] currentData, int signalIndex, int scanIndex,
                                         float[] mpsLims, float softGain)
        {
            int length = currentData[0][0].Length;
            int numAngles = currentData.Length;
            int numScanSteps = currentData[0].Length;

            int si = Math.Clamp(signalIndex, 0, numAngles - 1);
            int sj = Math.Clamp(scanIndex, 0, numScanSteps - 1);
            var line = currentData[si][sj];

            double x0 = mpsLims[0];
            double dx = (mpsLims[1] - mpsLims[0]) / Math.Max(1, (length - 1));
            double gain = (softGain == 0 ? 1.0 : softGain);

            using (LineDataSeries.SuspendUpdates())
            {
                LineDataSeries.Clear();
                for (int i = 0; i < length; i++)
                    LineDataSeries.Append(x0 + i * dx, line[i] * gain);
            }

            // keep axes stable if needed
            if (XAxis.AutoRange == AutoRange.Never)
                XAxis.VisibleRange = new DoubleRange(mpsLims[0], mpsLims[1]);

            // update percentage labels (in case amp range changed elsewhere)
            _ampMin = YAxisLeft.VisibleRange.MinAsDouble;
            _ampMax = YAxisLeft.VisibleRange.MaxAsDouble;
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
