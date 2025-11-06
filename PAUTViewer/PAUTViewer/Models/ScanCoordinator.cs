using PAUTViewer.ViewModels;
using PAUTViewer.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace PAUTViewer.Models
{
    public sealed class ScanCoordinator : IDisposable
    {
        private readonly ChannelContext _ctx;
        private readonly ScanState _st;
        private readonly AscanPAUserControl _a;
        private readonly BscanPAUserControl _b;
        private readonly CscanPAUserControl _c;
        private readonly DscanPAUserControl _d;


        public ScanCoordinator(ChannelContext ctx, ScanState st,
                           AscanPAUserControl a, BscanPAUserControl b,
                           CscanPAUserControl c, DscanPAUserControl d)
        {
            _ctx = ctx; _st = st; _a = a; _b = b; _c = c; _d = d;

            // --- wire once, here ---
            _c.LineMovedScan += OnCscanScanMoved;
            _c.LineMovedIndex += OnCscanIndexMoved;

            // If D-scan also has a scan line the user can drag, keep both in sync:
            _d.LineMovedScan += OnDscanScanMoved;

            _a.LineMovedMin += OnAscanGateMinMoved;
            _a.LineMovedMax += OnAscanGateMaxMoved;

            // initial paints
            UpdateAscan();
            UpdateBscan();
            UpdateCscan();
            UpdateDscan();
        }
        // ---- handlers ----
        private void OnCscanScanMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndex(scan);
            // keep D-scan line synced visually (optional)
            _d.UpdateScanLinePosition(scan);
            UpdateAscan(); UpdateBscan(); UpdateDscan();
        }

        private void OnDscanScanMoved(object? sender, float xWorld, int _)
        {
            // Mirror behavior: moving D-scan scan line updates state too
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndex(scan);
            _c.UpdateScanLinePosition(scan);
            UpdateAscan(); UpdateBscan(); UpdateDscan();
        }

        private void OnCscanIndexMoved(object? sender, float yWorld, int _)
        {
            int sample = WorldToNearestSample(yWorld, _ctx.Xlims, _ctx.Samples);
            _st.SetSampleIndex(sample);
            UpdateBscan();
        }

        private void OnAscanGateMinMoved(object? sender, float yWorld, int _)
        {
            var g0 = WorldDepthToIndex(yWorld, _ctx.Ylims, _ctx.Depths);
            _st.SetDepthGate(g0, _st.GateDepthMax);
            UpdateCscan(); UpdateDscan();
        }

        private void OnAscanGateMaxMoved(object? sender, float yWorld, int _)
        {
            var g1 = WorldDepthToIndex(yWorld, _ctx.Ylims, _ctx.Depths);
            _st.SetDepthGate(_st.GateDepthMin, g1);
            UpdateCscan(); UpdateDscan();
        }

        // ---- updates ----
        private void UpdateAscan()
        {
            _a.UpdateAscanPlotModel(_ctx.SigDps, _st.SampleIndex, _st.ScanIndex, _ctx.MpsLim, _st.Gain);
        }

        private void UpdateBscan()
        {
            _b.UpdateScanPlotModel(_ctx.SigDps, _st.ScanIndex, _ctx.Xlims, _ctx.Ylims, _st.Gain);
        }

        private void UpdateCscan()
        {
            _c.UpdateScanPlotModel(
                _ctx.SigDps,
                _st.GateDepthMin,
                _st.GateDepthMax,
                _st.Gain);
            // keep C-scan lines in sync with state (optional)
            _c.UpdateScanLinePosition(_st.ScanIndex);
            _c.UpdateIndexLinePosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleIndex / (double)(_ctx.Samples - 1)));
        }

        private void UpdateDscan()
        {
            _d.UpdateScanPlotModel(
                currentData: _ctx.SigDps,
                depthMinIdx: _st.GateDepthMin,
                depthMaxIdx: _st.GateDepthMax,
                ampRelMin: _st.AmpLimitMinRel,
                ampRelMax: _st.AmpLimitMaxRel,
                softGain: _st.Gain
            );
            _d.UpdateScanLinePosition(_st.ScanIndex);
        }

        // ---- utils ----
        private static int ClampToIndex(double worldX, int min, int max)
            => (int)Math.Clamp(Math.Round(worldX), min, max);

        private static int WorldDepthToIndex(double yWorld, float[] ylims, int depths)
        {
            double t = (yWorld - ylims[0]) / (ylims[1] - ylims[0]);
            return (int)Math.Clamp(Math.Round(t * (depths - 1)), 0, depths - 1);
        }

        private static int WorldToNearestSample(double xWorld, float[] xlims, int samples)
        {
            double t = (xWorld - xlims[0]) / (xlims[1] - xlims[0]);
            return (int)Math.Clamp(Math.Round(t * (samples - 1)), 0, samples - 1);
        }

        // ---- clean unhook ----
        public void Dispose()
        {
            _c.LineMovedScan -= OnCscanScanMoved;
            _c.LineMovedIndex -= OnCscanIndexMoved;
            _d.LineMovedScan -= OnDscanScanMoved;
            _a.LineMovedMin -= OnAscanGateMinMoved;
            _a.LineMovedMax -= OnAscanGateMaxMoved;
        }

    }
}
