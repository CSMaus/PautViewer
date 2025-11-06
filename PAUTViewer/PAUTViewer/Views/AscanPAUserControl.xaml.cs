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
        public delegate void LineMovedEventHandler(object sender, float newPosition, int channel);
        public event LineMovedEventHandler LineMovedMin;
        public event LineMovedEventHandler LineMovedMax;

        private int _channel;
        #endregion


        public AscanPAUserControl()
        {
            InitializeComponent();
            DataContext = this;
        }
        public void CreateAscanPlotModel(float[] xLims, float[] yLims, int channel)
        {
            _channel = channel;

            // axis ranges
            XAxis.VisibleRange = new DoubleRange(xLims[0], xLims[1]);
            YAxis.VisibleRange = new DoubleRange(yLims[0], yLims[1]);

            // place line inside range (center)
            double xMin = xLims[0] + 0.1 * (xLims[1] - xLims[0]);
            VLineMin.X1 = xMin;
            double xMax = xLims[0] + 0.9 * (xLims[1] - xLims[0]);
            VLineMax.X1 = xMax;

            // built-in drag event; IMPORTANT: X1 is IComparable → convert to double, clamp, assign back
            VLineMin.DragDelta += (_, __) =>
            {
                var rx = (DoubleRange)XAxis.VisibleRange;      // IRange → DoubleRange (has Min/Max doubles)
                double x = Convert.ToDouble(VLineMin.X1);         // IComparable → double
                // Clamp to axis visible range
                x = x < rx.Min ? rx.Min : (x > rx.Max ? rx.Max : x);
                VLineMin.X1 = x;                                  // assign back as IComparable
                LineMovedMin?.Invoke(this, (float)x, _channel);
            };
            VLineMax.DragDelta += (_, __) =>
            {
                var rx = (DoubleRange)XAxis.VisibleRange;      // IRange → DoubleRange (has Min/Max doubles)
                double x = Convert.ToDouble(VLineMax.X1);         // IComparable → double
                // Clamp to axis visible range
                x = x < rx.Min ? rx.Min : (x > rx.Max ? rx.Max : x);
                VLineMax.X1 = x;                                  // assign back as IComparable
                LineMovedMax?.Invoke(this, (float)x, _channel);
            };
        }

        public void UpdateAscanPlotModel(float[][][] currentData, int signalIndex, int scanIndex,
                                         float[] xLims, float softGain)
        {
            int length = currentData[0][0].Length;
            int numAngles = currentData.Length;
            int numScanSteps = currentData[0].Length;

            int si = Math.Clamp(signalIndex, 0, numAngles - 1);
            int sj = Math.Clamp(scanIndex, 0, numScanSteps - 1);
            var line = currentData[si][sj];

            double x0 = xLims[0];
            double dx = (xLims[1] - xLims[0]) / Math.Max(1, (length - 1));
            double gain = softGain == 0 ? 1.0 : softGain;

            using (LineDataSeries.SuspendUpdates())   // required SciChart pattern
            {
                LineDataSeries.Clear();
                for (int i = 0; i < length; i++)
                    LineDataSeries.Append(x0 + i * dx, line[i] * gain);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
