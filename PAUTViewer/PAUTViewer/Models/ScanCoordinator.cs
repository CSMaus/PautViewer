using PAUTViewer.ViewModels;
using PAUTViewer.Views;
using SciChart.Data.Model;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Events;

namespace PAUTViewer.Models
{
    public sealed class ScanCoordinator : IDisposable
    {
        private readonly ChannelContext _ctx;
        private readonly ScanState _st;
        private readonly AscanPAUserControl _a;
        private readonly BscanPAUserControl _b;
        private readonly DepthscanPAUserControl _d;
        private readonly CscanPAUserControl _ca;
        private readonly DscanPAUserControl _cp;
        private bool _isAxisSyncInternal;


        public ScanCoordinator(ChannelContext ctx, ScanState st,
                           AscanPAUserControl a, BscanPAUserControl b,
                           CscanPAUserControl ca, DscanPAUserControl cp, DepthscanPAUserControl d)
        {
            _ctx = ctx; _st = st; _a = a; _b = b; _ca = ca; _cp = cp; _d = d;

            // --- wire once, here ---
            _ca.LineMovedScanMax += OnCAscanScanMaxMoved;
            _ca.LineMovedIndexMax += OnCAscanIndexMaxMoved;
            _ca.LineMovedScanMin += OnCAscanScanMinMoved;
            _ca.LineMovedIndexMin += OnCAscanIndexMinMoved;

            _cp.LineMovedScanMax += OnCPscanScanMaxMoved;
            _cp.LineMovedScanMin += OnCPscanScanMinMoved;
            _cp.LineMovedIndexMax += OnCPscanIndexMaxMoved;
            _cp.LineMovedIndexMin += OnCPscanIndexMinMoved;


            _a.LineMovedMin += OnAscanGateMinMoved;
            _a.LineMovedMax += OnAscanGateMaxMoved;

            _d.LineMovedScanMax += OnDscanScanMaxMoved;
            _d.LineMovedScanMin += OnDscanScanMinMoved;
            _b.LineMovedIndexMax += OnBscanIndexMaxMoved;
            _b.LineMovedIndexMin += OnBscanIndexMinMoved;

            _st.SyncScansAxisChanged += OnSyncScansAxisChanged;
            _st.BscanRangeProjectionChanged += OnBscanRangeProjectionChanged;
            _st.DscanRangeProjectionChanged += OnDscanRangeProjectionChanged;
            UpdateAscan();
            UpdateBscan();
            UpdateCAscan();
            UpdateCPscan();
            UpdateDscan();
        }

        #region OnLineMoved haldlers funcitons for all
        private void OnCAscanScanMaxMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndexMax(scan);
            _cp.UpdateScanLineMaxPosition(scan);
            _d.UpdateScanLineMaxPosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }
        private void OnCAscanScanMinMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndexMin(scan);
            _cp.UpdateScanLineMinPosition(scan);
            _d.UpdateScanLineMinPosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }

        private void OnCPscanScanMaxMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndexMax(scan);
            _ca.UpdateScanLineMaxPosition(scan);
            _d.UpdateScanLineMaxPosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }
        private void OnCPscanScanMinMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndexMin(scan);
            _ca.UpdateScanLineMinPosition(scan);
            _d.UpdateScanLineMinPosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }

        private void OnDscanScanMaxMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndexMax(scan);
            _ca.UpdateScanLineMaxPosition(scan);
            _cp.UpdateScanLineMaxPosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }
        private void OnDscanScanMinMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndexMin(scan);
            _ca.UpdateScanLineMinPosition(scan);
            _cp.UpdateScanLineMinPosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }
        // C-scan handler
        private void OnCAscanIndexMaxMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndexMax(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _cp.UpdateIndexLineMaxPosition(ySnap);
            _b.UpdateIndexLineMaxPosition(ySnap);
            UpdateAscan(); UpdateDscan();  // UpdateBscan();
        }
        private void OnCAscanIndexMinMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndexMin(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _cp.UpdateIndexLineMinPosition(ySnap);
            _b.UpdateIndexLineMinPosition(ySnap);
            UpdateAscan(); UpdateDscan();  // UpdateBscan();
        }

        private void OnCPscanIndexMaxMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndexMax(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _ca.UpdateIndexLineMaxPosition(ySnap);
            _b.UpdateIndexLineMaxPosition(ySnap);

            UpdateAscan();
        }
        private void OnCPscanIndexMinMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndexMin(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _ca.UpdateIndexLineMinPosition(ySnap);
            _b.UpdateIndexLineMinPosition(ySnap);

            UpdateAscan();
        }
        private void OnBscanIndexMaxMoved(object? sender, float xWorld, int _)
        {
            int beam = WorldToNearestBeam(xWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndexMax(beam);

            double xSnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);

            _ca.UpdateIndexLineMaxPosition(xSnap);
            _cp.UpdateIndexLineMaxPosition(xSnap);

            UpdateAscan(); UpdateDscan();  // UpdateBscan();
        }
        private void OnBscanIndexMinMoved(object? sender, float xWorld, int _)
        {
            int beam = WorldToNearestBeam(xWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndexMin(beam);

            double xSnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);

            _ca.UpdateIndexLineMinPosition(xSnap);
            _cp.UpdateIndexLineMinPosition(xSnap);

            UpdateAscan(); UpdateDscan();  // UpdateBscan();
        }


        private void OnAscanGateMinMoved(object? sender, float xWorld, int _)
        {
            int g0 = WorldDepthToIndex_FromX(xWorld, _ctx.MpsLim, _ctx.DepthSamples);
            _st.SetDepthGate(g0, _st.GateDepthMax);
            _a.VLineMin.X1 = DepthIndexToWorldX(g0, _ctx.MpsLim, _ctx.DepthSamples);
            UpdateCAscan(); UpdateCPscan();
        }

        private void OnAscanGateMaxMoved(object? sender, float xWorld, int _)
        {
            int g1 = WorldDepthToIndex_FromX(xWorld, _ctx.MpsLim, _ctx.DepthSamples);
            _st.SetDepthGate(_st.GateDepthMin, g1);
            _a.VLineMax.X1 = DepthIndexToWorldX(g1, _ctx.MpsLim, _ctx.DepthSamples);
            UpdateCAscan(); UpdateCPscan();
        }

        #endregion

        #region Update Scans
        private void UpdateAscan()
        {
            _a.UpdateAscanPlotModel(_ctx.SigDps, _st.SampleMaxIndex, _st.ScanMaxIndex, _ctx.MpsLim, _st.Gain);
        }

        private void UpdateBscan()
        {
            _b.UpdateScanPlotModel(_ctx.SigDps, _st.ScanMinIndex, _st.ScanMaxIndex, _st.IsBscanRangeProjection, _ctx.Xlims, _ctx.Ylims, _st.Gain);

            double xSnapMax = BeamToWorld(_st.SampleMaxIndex, _ctx.Xlims, _ctx.Beams);
            double xSnapMin = BeamToWorld(_st.SampleMinIndex, _ctx.Xlims, _ctx.Beams);
            _b.UpdateIndexLineMaxPosition(xSnapMax);
            _b.UpdateIndexLineMinPosition(xSnapMin);
        }

        private void UpdateDscan()
        {
            // todo: here need to paste the positions of the index ranges later. Now it will be only one index line, -1 and false (for range projection)(
            _d.UpdateScanPlotModel(_ctx.SigDps, _st.SampleMinIndex, _st.SampleMaxIndex, _st.IsDscanRangeProjection, _ctx.ScanLims, _ctx.Ylims, _ctx.Alims[0], _ctx.Alims[1], _st.Gain);
            _d.UpdateScanLineMaxPosition(_st.ScanMaxIndex);
            _d.UpdateScanLineMinPosition(_st.ScanMinIndex);
        }

        private void UpdateCAscan()
        {
            _ca.UpdateScanPlotModel(
                _ctx.SigDps,
                _st.GateDepthMin,
                _st.GateDepthMax,
                _st.Gain);
            _ca.UpdateScanLineMaxPosition(_st.ScanMaxIndex);
            _ca.UpdateScanLineMinPosition(_st.ScanMinIndex);
            _ca.UpdateIndexLineMaxPosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleMaxIndex / (double)(_ctx.Beams - 1)));
            _ca.UpdateIndexLineMinPosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleMinIndex / (double)(_ctx.Beams - 1)));
        }

        private void UpdateCPscan()
        {
            _cp.UpdateScanPlotModel(
                currentData: _ctx.SigDps,
                depthMinIdx: _st.GateDepthMin,
                depthMaxIdx: _st.GateDepthMax,
                Alims: _ctx.Alims,
                softGain: _st.Gain
            );
            _cp.UpdateScanLineMaxPosition(_st.ScanMaxIndex);
            _cp.UpdateScanLineMinPosition(_st.ScanMinIndex);
            _cp.UpdateIndexLineMaxPosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleMaxIndex / (double)(_ctx.Beams - 1)));
            _cp.UpdateIndexLineMinPosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleMinIndex / (double)(_ctx.Beams - 1)));
        }

        private void OnBscanRangeProjectionChanged(bool enabled)
        {
            UpdateBscan();
        }
        private void OnDscanRangeProjectionChanged(bool enabled)
        {
            UpdateDscan();
        }
        #endregion

        #region Sync Axis
        private void OnSyncScansAxisChanged(bool enabled)
        {
            if (enabled)
                EnableAxisSync();
            else
                DisableAxisSync();
        }
        private void EnableAxisSync()
        {
            // 1) Scan axis (X) – C-scan is master
            var scanRange = _ca.XAxisControl.VisibleRange;
            _cp.XAxisControl.VisibleRange = scanRange;
            _d.XAxisControl.VisibleRange = scanRange;

            // 2) Index axis – C-scan Y is master
            var indexRange = _ca.YAxisControl.VisibleRange;
            _b.YAxisControl.VisibleRange = indexRange;
            _cp.YAxisControl.VisibleRange = indexRange;

            // 3) Depth axis – B-scan X is master
            var depthRange = _b.XAxisControl.VisibleRange;
            _d.YAxisControl.VisibleRange = depthRange;
            _a.XAxisControl.VisibleRange = depthRange;
            SubscribeAxisSyncEvents();
        }

        private void DisableAxisSync()
        {
            UnsubscribeAxisSyncEvents();
            var rCpX = _cp.XAxisControl.VisibleRange;
            _cp.XAxisControl.VisibleRange = new DoubleRange(
                (double)rCpX.Min, (double)rCpX.Max);

            var rDX = _d.XAxisControl.VisibleRange;
            _d.XAxisControl.VisibleRange = new DoubleRange(
                (double)rDX.Min, (double)rDX.Max);

            var rBY = _b.YAxisControl.VisibleRange;
            _b.YAxisControl.VisibleRange = new DoubleRange(
                (double)rBY.Min, (double)rBY.Max);

            var rCpY = _cp.YAxisControl.VisibleRange;
            _cp.YAxisControl.VisibleRange = new DoubleRange(
                (double)rCpY.Min, (double)rCpY.Max);

            var rDY = _d.YAxisControl.VisibleRange;
            _d.YAxisControl.VisibleRange = new DoubleRange(
                (double)rDY.Min, (double)rDY.Max);

            var rAX = _a.XAxisControl.VisibleRange;
            _a.XAxisControl.VisibleRange = new DoubleRange(
                (double)rAX.Min, (double)rAX.Max);
        }

        private void SubscribeAxisSyncEvents()
        {
            _ca.XAxisControl.VisibleRangeChanged += OnScanAxisVisibleRangeChanged;
            _cp.XAxisControl.VisibleRangeChanged += OnScanAxisVisibleRangeChanged;
            _d.XAxisControl.VisibleRangeChanged += OnScanAxisVisibleRangeChanged;

            _ca.YAxisControl.VisibleRangeChanged += OnIndexAxisVisibleRangeChanged;
            _b.YAxisControl.VisibleRangeChanged += OnIndexAxisVisibleRangeChanged;
            _cp.YAxisControl.VisibleRangeChanged += OnIndexAxisVisibleRangeChanged;

            _b.XAxisControl.VisibleRangeChanged += OnDepthAxisVisibleRangeChanged;
            _d.YAxisControl.VisibleRangeChanged += OnDepthAxisVisibleRangeChanged;
            _a.XAxisControl.VisibleRangeChanged += OnDepthAxisVisibleRangeChanged;
        }
        private void UnsubscribeAxisSyncEvents()
        {
            _ca.XAxisControl.VisibleRangeChanged -= OnScanAxisVisibleRangeChanged;
            _cp.XAxisControl.VisibleRangeChanged -= OnScanAxisVisibleRangeChanged;
            _d.XAxisControl.VisibleRangeChanged -= OnScanAxisVisibleRangeChanged;

            _ca.YAxisControl.VisibleRangeChanged -= OnIndexAxisVisibleRangeChanged;
            _b.YAxisControl.VisibleRangeChanged -= OnIndexAxisVisibleRangeChanged;
            _cp.YAxisControl.VisibleRangeChanged -= OnIndexAxisVisibleRangeChanged;

            _b.XAxisControl.VisibleRangeChanged -= OnDepthAxisVisibleRangeChanged;
            _d.YAxisControl.VisibleRangeChanged -= OnDepthAxisVisibleRangeChanged;
            _a.XAxisControl.VisibleRangeChanged -= OnDepthAxisVisibleRangeChanged;

        }
        private void OnScanAxisVisibleRangeChanged(object sender, VisibleRangeChangedEventArgs e)
        {
            if (!_st.IsSyncScansAxis || _isAxisSyncInternal)
                return;

            _isAxisSyncInternal = true;
            try
            {
                var newRange = e.NewVisibleRange;
                var dr = new DoubleRange((double)newRange.Min, (double)newRange.Max);

                // Scan axis group: CA, CP, D share X
                if (sender == _ca.XAxisControl)
                {
                    _cp.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _d.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
                else if (sender == _cp.XAxisControl)
                {
                    _ca.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _d.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
                else if (sender == _d.XAxisControl)
                {
                    _ca.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _cp.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
            }
            finally
            {
                _isAxisSyncInternal = false;
            }
        }

        private void OnIndexAxisVisibleRangeChanged(object sender, VisibleRangeChangedEventArgs e)
        {
            if (!_st.IsSyncScansAxis || _isAxisSyncInternal)
                return;

            _isAxisSyncInternal = true;
            try
            {
                var newRange = e.NewVisibleRange;
                var dr = new DoubleRange((double)newRange.Min, (double)newRange.Max);

                // Index axis group: CA.Y, B.Y, CP.Y
                if (sender == _ca.YAxisControl)
                {
                    _b.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _cp.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
                else if (sender == _b.YAxisControl)
                {
                    _ca.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _cp.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
                else if (sender == _cp.YAxisControl)
                {
                    _ca.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _b.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
            }
            finally
            {
                _isAxisSyncInternal = false;
            }
        }

        private void OnDepthAxisVisibleRangeChanged(object sender, VisibleRangeChangedEventArgs e)
        {
            if (!_st.IsSyncScansAxis || _isAxisSyncInternal)
                return;

            _isAxisSyncInternal = true;
            try
            {
                var newRange = e.NewVisibleRange;
                var dr = new DoubleRange((double)newRange.Min, (double)newRange.Max);

                // Depth axis group: B.X, D.Y, A.X
                if (sender == _b.XAxisControl)
                {
                    _d.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _a.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
                else if (sender == _d.YAxisControl)
                {
                    _b.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _a.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
                else if (sender == _a.XAxisControl)
                {
                    _b.XAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                    _d.YAxisControl.VisibleRange = new DoubleRange(dr.Min, dr.Max);
                }
            }
            finally
            {
                _isAxisSyncInternal = false;
            }
        }

        #endregion

        #region Utils and Clean
        private static int ClampToIndex(double worldX, int min, int max)
            => (int)Math.Clamp(Math.Round(worldX), min, max);


        private static int WorldToNearestBeam(double yWorld, float[] idxLims, int beams)
        {
            double t = (yWorld - idxLims[0]) / (idxLims[1] - idxLims[0]);
            return (int)Math.Clamp(Math.Round(t * (beams - 1)), 0, beams - 1);
        }
        private static double BeamToWorld(int beam, float[] idxLims, int beams)
        {
            if (beams <= 1) return idxLims[0];
            double t = beam / (double)(beams - 1);
            return idxLims[0] + t * (idxLims[1] - idxLims[0]);
        }
        private static int WorldDepthToIndex_FromX(double xWorld, float[] xlims, int depthSamples)
        {
            double t = (xWorld - xlims[0]) / (xlims[1] - xlims[0]);
            return (int)Math.Clamp(Math.Round(t * (depthSamples - 1)), 0, depthSamples - 1);
        }
        private static double DepthIndexToWorldX(int d, float[] xlims, int depthSamples)
        {
            if (depthSamples <= 1) return xlims[0];
            double t = d / (double)(depthSamples - 1);
            return xlims[0] + t * (xlims[1] - xlims[0]);
        }

        public void Dispose()
        {
            _ca.LineMovedScanMax -= OnCAscanScanMaxMoved;
            _ca.LineMovedScanMin -= OnCAscanScanMinMoved;
            _ca.LineMovedIndexMax -= OnCAscanIndexMaxMoved;
            _ca.LineMovedIndexMin -= OnCAscanIndexMinMoved;

            _cp.LineMovedScanMax -= OnCPscanScanMaxMoved;
            _cp.LineMovedScanMin -= OnCPscanScanMinMoved;
            _cp.LineMovedIndexMax -= OnCPscanIndexMaxMoved;
            _cp.LineMovedIndexMin -= OnCPscanIndexMinMoved;

            _d.LineMovedScanMax -= OnDscanScanMaxMoved;
            _d.LineMovedScanMin -= OnDscanScanMinMoved;
            _b.LineMovedIndexMax -= OnBscanIndexMaxMoved;
            _b.LineMovedIndexMin -= OnBscanIndexMinMoved;
            _a.LineMovedMin -= OnAscanGateMinMoved;
            _a.LineMovedMax -= OnAscanGateMaxMoved;
            UnsubscribeAxisSyncEvents();
        }
        #endregion

    }
}
