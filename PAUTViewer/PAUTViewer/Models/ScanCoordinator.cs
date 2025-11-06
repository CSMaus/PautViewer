using PAUTViewer.ViewModels;
using PAUTViewer.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;

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
            _d.LineMovedIndex += OnDscanIndexMoved;

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
            _d.UpdateScanLinePosition(scan); // keep D-scan line synced visually
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

        // C-scan handler
        private void OnCscanIndexMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndex(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _d.UpdateIndexLinePosition(ySnap);   // keep visual in sync
            _c.UpdateIndexLinePosition(ySnap);

            UpdateBscan(); UpdateAscan();
        }

        // D-scan handler
        private void OnDscanIndexMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndex(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _c.UpdateIndexLinePosition(ySnap);
            _d.UpdateIndexLinePosition(ySnap);

            UpdateBscan(); UpdateAscan();
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

        private void OnAscanGateMinMoved(object? sender, float xWorld, int _)
        {
            int g0 = WorldDepthToIndex_FromX(xWorld, _ctx.MpsLim, _ctx.DepthSamples);
            _st.SetDepthGate(g0, _st.GateDepthMax);
            _a.VLineMin.X1 = DepthIndexToWorldX(g0, _ctx.MpsLim, _ctx.DepthSamples);
            UpdateCscan(); UpdateDscan();
        }

        private void OnAscanGateMaxMoved(object? sender, float xWorld, int _)
        {
            int g1 = WorldDepthToIndex_FromX(xWorld, _ctx.MpsLim, _ctx.DepthSamples);
            _st.SetDepthGate(_st.GateDepthMin, g1);
            _a.VLineMax.X1 = DepthIndexToWorldX(g1, _ctx.MpsLim, _ctx.DepthSamples);
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
            _c.UpdateIndexLinePosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleIndex / (double)(_ctx.Beams - 1)));
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
            _d.UpdateIndexLinePosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleIndex / (double)(_ctx.Beams - 1)));
        }

        // ---- utils ----
        private static int ClampToIndex(double worldX, int min, int max)
            => (int)Math.Clamp(Math.Round(worldX), min, max);

        
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
