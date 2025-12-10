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
        private readonly DepthscanPAUserControl _d;
        private readonly CscanPAUserControl _ca;
        private readonly DscanPAUserControl _cp;


        public ScanCoordinator(ChannelContext ctx, ScanState st,
                           AscanPAUserControl a, BscanPAUserControl b,
                           CscanPAUserControl ca, DscanPAUserControl cp, DepthscanPAUserControl d)
        {
            _ctx = ctx; _st = st; _a = a; _b = b; _ca = ca; _cp = cp; _d = d;

            // --- wire once, here ---
            _ca.LineMovedScanMax += OnCAscanScanMoved;
            _ca.LineMovedIndexMax += OnCAscanIndexMoved;

            _cp.LineMovedScan += OnCPscanScanMoved;
            _cp.LineMovedIndex += OnCPscanIndexMoved;


            _a.LineMovedMin += OnAscanGateMinMoved;
            _a.LineMovedMax += OnAscanGateMaxMoved;

            _d.LineMovedScanMax += OnDscanScanMoved;
            _b.LineMovedIndex += OnBscanIndexMoved;

            UpdateAscan();
            UpdateBscan();
            UpdateCAscan();
            UpdateCPscan();
            UpdateDscan();
        }

        #region OnLineMoved haldlers funcitons for all
        private void OnCAscanScanMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndex(scan);
            _cp.UpdateScanLinePosition(scan);
            _d.UpdateScanLinePosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }

        private void OnCPscanScanMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndex(scan);
            _ca.UpdateScanLinePosition(scan);
            _d.UpdateScanLinePosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }

        private void OnDscanScanMoved(object? sender, float xWorld, int _)
        {
            int scan = ClampToIndex(xWorld, _ctx.ScanLims[0], _ctx.ScanLims[1] - 1);
            _st.SetScanIndex(scan);
            _ca.UpdateScanLinePosition(scan);
            _cp.UpdateScanLinePosition(scan);
            UpdateAscan(); UpdateBscan(); // UpdateCPscan();
        }
        // C-scan handler
        private void OnCAscanIndexMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndex(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _cp.UpdateIndexLinePosition(ySnap);
            _b.UpdateIndexLinePosition(ySnap);
            UpdateAscan(); UpdateDscan();  // UpdateBscan();
        }

        private void OnCPscanIndexMoved(object? sender, float yWorld, int _)
        {
            int beam = WorldToNearestBeam(yWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndex(beam);

            double ySnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);
            _ca.UpdateIndexLineMaxPosition(ySnap);
            _b.UpdateIndexLinePosition(ySnap);

            UpdateAscan();
        }
        private void OnBscanIndexMoved(object? sender, float xWorld, int _)
        {
            int beam = WorldToNearestBeam(xWorld, _ctx.Xlims, _ctx.Beams);
            _st.SetSampleIndex(beam);

            double xSnap = BeamToWorld(beam, _ctx.Xlims, _ctx.Beams);

            _ca.UpdateIndexLineMaxPosition(xSnap);
            _cp.UpdateIndexLinePosition(xSnap);

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

        #region Updates
        private void UpdateAscan()
        {
            _a.UpdateAscanPlotModel(_ctx.SigDps, _st.SampleIndex, _st.ScanIndex, _ctx.MpsLim, _st.Gain);
        }

        private void UpdateBscan()
        {
            _b.UpdateScanPlotModel(_ctx.SigDps, _st.ScanIndex, _ctx.Xlims, _ctx.Ylims, _st.Gain);

            double xSnap = BeamToWorld(_st.SampleIndex, _ctx.Xlims, _ctx.Beams);
            _b.UpdateIndexLinePosition(xSnap);
        }

        private void UpdateDscan()
        {
            // todo: here need to paste the positions of the index ranges later. Now it will be only one index line, -1 and false (for range projection)(
            _d.UpdateScanPlotModel(_ctx.SigDps, _st.SampleIndex, -1, false, _ctx.ScanLims, _ctx.Ylims, _ctx.Alims[0], _ctx.Alims[1], _st.Gain);
            _d.UpdateScanLinePosition(_st.ScanIndex);
        }

        private void UpdateCAscan()
        {
            _ca.UpdateScanPlotModel(
                _ctx.SigDps,
                _st.GateDepthMin,
                _st.GateDepthMax,
                _st.Gain);
            _ca.UpdateScanLinePosition(_st.ScanIndex);
            _ca.UpdateIndexLineMaxPosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleIndex / (double)(_ctx.Beams - 1)));
        }

        private void UpdateCPscan()
        {
            _cp.UpdateScanPlotModel(
                currentData: _ctx.SigDps,
                depthMinIdx: _st.GateDepthMin,
                depthMaxIdx: _st.GateDepthMax,
                ampRelMin: _st.AmpLimitMinRel,
                ampRelMax: _st.AmpLimitMaxRel,
                softGain: _st.Gain
            );
            _cp.UpdateScanLinePosition(_st.ScanIndex);
            _cp.UpdateIndexLinePosition(_ctx.Xlims[0] + (_ctx.Xlims[1] - _ctx.Xlims[0]) * (_st.SampleIndex / (double)(_ctx.Beams - 1)));
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
            _ca.LineMovedScanMax -= OnCAscanScanMoved;
            _ca.LineMovedIndexMax -= OnCAscanIndexMoved;
            _cp.LineMovedScan -= OnCPscanScanMoved;
            _cp.LineMovedIndex -= OnCPscanIndexMoved;
            _d.LineMovedScanMax -= OnDscanScanMoved;
            _b.LineMovedIndex -= OnBscanIndexMoved;
            _a.LineMovedMin -= OnAscanGateMinMoved;
            _a.LineMovedMax -= OnAscanGateMaxMoved;
        }
        #endregion

    }
}
