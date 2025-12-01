using OlympusNDT.Instrumentation.NET;
using OlympusNDT.Storage.NET;
using PAUTViewer.Models;
using PAUTViewer.ProjectUtilities;
using PAUTViewer.Views;
using SciChart.Charting.Visuals.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq; // <-- needed for ToList(), Min/Max LINQ, etc.
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ToastNotifications.Messages;
using System.IO;
using System.Text.Json;

namespace PAUTViewer.ViewModels
{
    public class PlotPAViewModel
    {
        #region Initial Variables
        public string FilePath { get; set; }
        public string FileInfo { get; set; }

        private ObservableCollection<string> _fileInfoList;
        public ObservableCollection<string> FileInfoList
        {
            get { return _fileInfoList; }
            set
            {
                if (_fileInfoList != value)
                {
                    _fileInfoList = value;
                    OnPropertyChanged(nameof(FileInfoList));
                }
            }
        }

        private int[] channels;

        private string _depthMin;
        public string DepthMin { get => _depthMin; set { _depthMin = value; OnPropertyChanged(nameof(DepthMin)); } }

        private string _depthMax;
        public string DepthMax { get => _depthMax; set { _depthMax = value; OnPropertyChanged(nameof(DepthMax)); } }

        public List<float[]> Tofs { get; set; }
        public List<float[]> Mps { get; set; }
        public List<float[]> Depths { get; set; }
        public List<float[]> Dists { get; set; }

        public List<float[,]> CscanSig = new List<float[,]>();
        public List<int[]> ScanLims = new List<int[]>();
        public List<float[][][]> SigDps { get; set; }
        public List<int[]> _sigDpsLengths { get; private set; }

        public List<float[]> Angles { get; set; }
        public List<float[]> Alims { get; set; }
        public List<float[]> Indexes { get; set; }
        public List<float[]> Xlims { get; private set; }
        public List<float[]> Ylims { get; private set; }
        public List<float[]> MpsLim { get; private set; }
        public List<float[]> TofLim { get; private set; }
        private List<float> ScanStep { get; set; }

        private uint numAngles { get; set; }
        public int numChannels { get; set; }
        private float SoundVel;
        Stopwatch stopwatch = new Stopwatch();
        public bool IsResettingGUI { get; set; } = false;

        private List<string> _exportOptions;
        public List<string> ExportOptions
        {
            get { return _exportOptions; }
            set
            {
                _exportOptions = value;
                OnPropertyChanged(nameof(ExportOptions));
            }
        }



        private InspectionFileInfo _aiInspectionFileInfo;
        public InspectionFileInfo aiInspectionFileInfo
        {
            get { return _aiInspectionFileInfo; }
            set
            {
                _aiInspectionFileInfo = value;
                OnPropertyChanged(nameof(aiInspectionFileInfo));
            }
        }

        public ICommand RecalculateSize_Command { get; set; }

        private ReportData _reportData;
        public ReportData _ReportData
        {
            get { return _reportData; }
            set
            {
                _reportData = value;
                OnPropertyChanged(nameof(_ReportData));
            }
        }

        private string _defectSizeThreshold;
        public string DefectSizeThreshold
        {
            get { return _defectSizeThreshold; }
            set
            {
                _defectSizeThreshold = value;
                OnPropertyChanged(nameof(DefectSizeThreshold));
            }
        }
        public List<string> InspectionMethods { get; } = Enum.GetNames(typeof(InspectionMethodType)).ToList();

        #endregion

        #region Scans Fields and Variables
        private readonly List<ScanCoordinator> _coordinators = new();

        private List<string> _configNames;
        public List<string> configNames
        {
            get { return _configNames; }
            set
            {
                _configNames = value;
                OnPropertyChanged(nameof(configNames));
            }
        }

        private int signalIndex = 10;
        private int scanIndex = 0;
        private bool isUpdatingLinkedLinesDepth = false;
        private bool isUpdatingLinkedLinesScan = false;
        private bool isUpdatingSelectedRect = false;

        #endregion

        private DataLoader loadedData;
        public PlotPAViewModel(DataLoader loadedData)
        {
            this.loadedData = loadedData;
            WriteLoadedDataIntoVariables(loadedData);
            PlotData();
        }

        public void WriteLoadedDataIntoVariables(DataLoader loadedData)
        {
            if (loadedData == null || loadedData.FileInformation == null)
            {
                Console.WriteLine("Data is not loaded");
                return;
            }

            FilePath = loadedData.FilePath;
            FileInfo = string.Join(Environment.NewLine, loadedData.FileInformation);
            LoadFiles(loadedData.FileInformation);

            Tofs = loadedData.Tofs;    // time in μs
            SigDps = loadedData.SigDps; // float[numAngles][numScanSteps][lenSignal] (your jagged 3D)

            Angles = loadedData.Angles;
            Alims = loadedData.Alims;
            configNames = loadedData.configNames;
            Indexes = loadedData.Indexes;  // offset
            SoundVel = loadedData.SoundVel;
            CscanSig = loadedData.CscanSig;
            ScanLims = loadedData.ScanLims;
            numAngles = loadedData.numAngles;
            ScanStep = loadedData.ScanStep;

            numChannels = configNames.Count();

            Mps = new List<float[]>(numChannels);
            Depths = new List<float[]>(numChannels);
            Dists = new List<float[]>(numChannels);

            Xlims = new List<float[]>(numChannels);
            Ylims = new List<float[]>(numChannels);
            MpsLim = new List<float[]>(numChannels);
            TofLim = new List<float[]>(numChannels);
            _sigDpsLengths = new List<int[]>(numChannels);

            aiInspectionFileInfo = new InspectionFileInfo();
            channels = new int[numChannels];

            for (int ichan = 0; ichan < numChannels; ichan++)
            {
                int getLen0 = SigDps[ichan].Length;         // samples (signal index)
                int getLen1 = SigDps[ichan][0].Length;      // scans
                int getLen2 = SigDps[ichan][0][0].Length;   // depth (A-scan length)

                _sigDpsLengths.Add(new int[] { getLen0, getLen1, getLen2 });

                numAngles = (uint)Angles[ichan].Count();
                var maxAngle = (Angles.Count() < 1) ? 0 : Angles[ichan].Max();
                channels[ichan] = ichan;

                var tofs = Tofs[ichan];
                float[] mps = tofs.Select(tof => tof / 2 * SoundVel / 1000).ToArray();
                Mps.Add(mps);

                if (numAngles == 1)
                {
                    float[] depths = mps.Select(mp => mp * (float)Math.Cos(ToRadians(maxAngle))).ToArray();
                    Depths.Add(depths);
                    Dists.Add(Indexes[ichan]);
                }
                if (numAngles > 1)
                {
                    var angles = Angles[ichan];
                    var indexes = Indexes[ichan];

                    var A = angles;
                    var O = indexes;

                    float mpsMin = Mps[ichan].Length > 0 ? Mps[ichan].Min() : 0f;
                    float mpsMax = Mps[ichan].Length > 0 ? Mps[ichan].Max() : 0f;

                    var (xMin, xMax, yMin, yMax) = ComputeExtents(mpsMin, mpsMax, A, O);

                    int xCount = angles.Length;
                    int yCount = Mps[ichan].Length;

                    Depths.Add(Linspace(yMin, yMax, yCount).Select(d => (float)d).ToArray());
                    Dists.Add(Linspace(xMin, xMax, xCount).Select(d => (float)d).ToArray());
                }
                else
                {
                    // optional: notify single-angle case
                }

                float[] xlims = new float[2];
                float[] ylims = new float[2];

                if (Dists != null)
                {
                    xlims[0] = Dists[ichan][0];
                    xlims[1] = Dists[ichan][Dists[ichan].GetLength(0) - 1];
                }
                else
                {
                    xlims[0] = Tofs[ichan][0];
                    xlims[1] = Tofs[ichan][Tofs[ichan].GetLength(0) - 1];
                }

                if (Depths != null)
                {
                    ylims[0] = Depths[ichan][0];
                    ylims[1] = Depths[ichan][Depths[ichan].GetLength(0) - 1];
                }
                else
                {
                    ylims[0] = mps[0];
                    ylims[1] = mps[mps.Length - 1];
                }
                MpsLim.Add(new float[] { mps[0], mps[mps.Length - 1] });
                TofLim.Add(new float[] { Tofs[ichan][0], Tofs[ichan][Tofs[ichan].GetLength(0) - 1] });

                // check the ranges
                EnsureAscending(xlims);
                EnsureAscending(ylims);
                EnsureAscending(MpsLim[ichan]);
                EnsureAscending(TofLim[ichan]);

                Xlims.Add(xlims);
                Ylims.Add(ylims);

                Xlims.Add(xlims);
                Ylims.Add(ylims);
            }
        }

        public void LoadFiles(IEnumerable<string> lines)
        {
            if (FileInfoList != null) FileInfoList.Clear();
            FileInfoList = new ObservableCollection<string>(lines);
        }

        string toSaveSignalsPath_config = "";

        // tabs container (used later in PlotData)
        public ChannelUI Current
        {
            get
            {
                var i = Math.Clamp(SelectedConfigIndex, 0, Math.Max(0, Channels.Count - 1));
                return Channels.Count > 0 ? Channels[i] : null;
            }
        }
        public ObservableCollection<ChannelUI> Channels { get; } = new();
        private int _selectedConfigIndex = 0;
        public int SelectedConfigIndex
        {
            get => _selectedConfigIndex;
            set
            {
                if (_selectedConfigIndex == value) return;
                _selectedConfigIndex = value;
                OnPropertyChanged(nameof(SelectedConfigIndex));
                OnPropertyChanged(nameof(Current));           // <— important
            }
        }


        public void PlotData()
        {
            if (FilePath == null || configNames == null)
            {
                Console.WriteLine("Data is not loaded");
                return;
            }
            if (SigDps.Count < 1)
            {
                string notificationText = Application.Current.Resources["notificationDataNotLoaded"] as string;
                NotificationManager.Notifier.ShowError(notificationText);
                return;
            }

            foreach (int ichan in channels)
            {
                var a = new AscanPAUserControl();
                a.CreateAscanPlotModel(MpsLim[ichan], Alims[ichan], ichan);
                a.UpdateAscanPlotModel(SigDps[ichan],
                    _sigDpsLengths[ichan][0] / 2,
                    _sigDpsLengths[ichan][1] / 2,
                    MpsLim[ichan], 1);

                var c = new CscanPAUserControl();
                c.CreateScanPlotModel(ichan, ScanLims[ichan], Xlims[ichan], Alims[ichan][1],
                                      _sigDpsLengths[ichan][1], _sigDpsLengths[ichan][0], ScanStep[ichan]);
                c.UpdateScanPlotModel(SigDps[ichan], -1, -1);

                var d = new DscanPAUserControl();
                d.CreateScanPlotModel(
                    channel: ichan,
                    scansLims: ScanLims[ichan],                 // X
                    xlims: Xlims[ichan],                        // Y
                    depthWorldRange: (Ylims[ichan][0], Ylims[ichan][1]),  // Z colormap
                    ampMaxAbs: Alims[ichan][1],
                    scanCount: _sigDpsLengths[ichan][1],
                    sampleCount: _sigDpsLengths[ichan][0],
                    scanStep: ScanStep[ichan],
                    indexStep: 1.0
                );
                d.UpdateScanPlotModel(SigDps[ichan], 0, _sigDpsLengths[ichan][2], 0.3, 1.0, 1f);

                var b = new BscanPAUserControl();
                b.CreateScanPlotModel(ichan, Ylims[ichan], Xlims[ichan], Alims[ichan][1]);
                b.UpdateScanPlotModel(SigDps[ichan], (int)ScanLims[ichan][0] + 1, Xlims[ichan], Ylims[ichan], 1);

                // Coordinator wiring (NO ad-hoc "+=" handlers)
                var ctx = new ChannelContext(ichan, SigDps[ichan], MpsLim[ichan], Xlims[ichan], Ylims[ichan], ScanLims[ichan], Alims[ichan]);
                var st = new ScanState();
                st.SetScanIndex(_sigDpsLengths[ichan][1] / 2);
                st.SetSampleIndex(_sigDpsLengths[ichan][0] / 2);
                st.SetDepthGate(0, _sigDpsLengths[ichan][2] - 1);
                st.SetGain(1f);
                var coord = new ScanCoordinator(ctx, st, a, b, c, d);

                Channels.Add(new ChannelUI
                {
                    Channel = ichan,
                    Ascan = a,
                    Bscan = b,
                    Cscan = c,
                    Dscan = d,
                    Context = ctx,
                    State = st,
                    Coordinator = coord,
                    RowHeight1 = new GridLength(3, GridUnitType.Star), // left top
                    RowHeight2 = new GridLength(15),                   // left horiz splitter
                    RowHeight3 = new GridLength(1, GridUnitType.Star), // left bottom
                    RowHeightBottom = GridLength.Auto,
                    ColumnWidth1 = new GridLength(1, GridUnitType.Star), // left
                    ColumnWidth2 = new GridLength(15),                   // vert splitter
                    ColumnWidth3 = new GridLength(1, GridUnitType.Star), // right
                });
            }

            _selectedConfigIndex = 0;
            SelectedConfigIndex = 0;

            FillAIInspectionFileInfo();
            foreach (int ichan in channels)
            {
                //RecalculateDscanGates(false, ichan);
            }
            _ReportData = new ReportData();
        }

        private void FillAIInspectionFileInfo()
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(FilePath);
            string folderPath = System.IO.Path.GetDirectoryName(FilePath);
            string _specimenName = fileName.Contains("_") ? fileName.Substring(0, fileName.LastIndexOf("_")) : "";
            string _company = "No Company Name";
            var _dateOfInspection = DateTime.Today;
            var _dateOfAnalysis = DateTime.Now;

            InspectionFileInfo inf = new InspectionFileInfo
            {
                File = fileName,
                Folder = folderPath,
                SpecimenName = _specimenName,
                Company = _company,
                DateOfInspection = _dateOfInspection,
                DateOfAnalysis = _dateOfAnalysis,
                UserName = "UserName",
                Password = "Password",
                DBName = "Database Name"
            };

            ExportOptions = new List<string>() { "Export CSV", "Export DB" };

            aiInspectionFileInfo = inf;
        }


        #region Export data into text file (3 formats)

        public void SaveData2FileExt(string saveFilePath, string extension)
        {
            // for each channel save all data file that we have
            // saveFilePath already contains extension, but need to choose extension from "extension" variable, if there is a difference between json, csv and txt saving ways
            if (string.IsNullOrWhiteSpace(saveFilePath) || numChannels <= 0 || SigDps == null) 
            {
                NotificationManager.Notifier.ShowWarning($"Save file path is not specified");
                return;
            }
                

            var ext = extension;
            if (string.IsNullOrWhiteSpace(ext))
                ext = Path.GetExtension(saveFilePath);

            if (string.IsNullOrWhiteSpace(ext))
                ext = ".json";

            if (!ext.StartsWith(".")) ext = "." + ext;

            string dir = Path.GetDirectoryName(saveFilePath);
            if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
            string baseName = Path.GetFileNameWithoutExtension(saveFilePath);

            for (int ichan = 0; ichan < numChannels; ichan++)
            {
                string cfgName = (configNames != null && ichan < configNames.Count)
                    ? configNames[ichan]
                    : $"CH{ichan}";

                // sanitize config name for file name
                foreach (var c in Path.GetInvalidFileNameChars())
                    cfgName = cfgName.Replace(c, '_');

                string chanFileName = $"{baseName}_CH{ichan}_{cfgName}{ext}";
                string chanPath = Path.Combine(dir, chanFileName);

                switch (ext.ToLowerInvariant())
                {
                    case ".json":
                        SaveChannelAsJson(ichan, chanPath);
                        break;
                    case ".csv":
                        SaveChannelAsCsv(ichan, chanPath, separator: ',');
                        break;
                    case ".txt":
                        SaveChannelAsCsv(ichan, chanPath, separator: '\t');
                        break;
                    default:
                        // fallback: json
                        SaveChannelAsJson(ichan, chanPath);
                        break;
                }
            }
            NotificationManager.Notifier.ShowSuccess($"Data saved to: {saveFilePath}");
        }
        private void SaveChannelAsJson(int ichan, string path)
        {
            // Safeguards
            if (SigDps == null || ichan < 0 || ichan >= SigDps.Count)
                return;

            var sig = SigDps[ichan];
            int beams = sig.Length;
            int scans = beams > 0 ? sig[0].Length : 0;
            int depthSamples = (scans > 0) ? sig[0][0].Length : 0;

            float[] xlims = (Xlims != null && ichan < Xlims.Count) ? Xlims[ichan] : null;
            float[] ylims = (Ylims != null && ichan < Ylims.Count) ? Ylims[ichan] : null;
            float[] mpsLim = (MpsLim != null && ichan < MpsLim.Count) ? MpsLim[ichan] : null;
            float[] tofLim = (TofLim != null && ichan < TofLim.Count) ? TofLim[ichan] : null;
            int[] scanLims = (ScanLims != null && ichan < ScanLims.Count) ? ScanLims[ichan] : null;

            float[] tofs = (Tofs != null && ichan < Tofs.Count) ? Tofs[ichan] : null;
            float[] mps = (Mps != null && ichan < Mps.Count) ? Mps[ichan] : null;
            float[] depths = (Depths != null && ichan < Depths.Count) ? Depths[ichan] : null;
            float[] dists = (Dists != null && ichan < Dists.Count) ? Dists[ichan] : null;
            float[] angles = (Angles != null && ichan < Angles.Count) ? Angles[ichan] : null;
            float[] alims = (Alims != null && ichan < Alims.Count) ? Alims[ichan] : null;
            float[] indexes = (Indexes != null && ichan < Indexes.Count) ? Indexes[ichan] : null;

            float[,] cscan = (CscanSig != null && ichan < CscanSig.Count) ? CscanSig[ichan] : null;
            float[][] cscanJagged = cscan != null ? ToJagged2D(cscan) : null;

            float scanStep = (ScanStep != null && ichan < ScanStep.Count) ? ScanStep[ichan] : 0f;

            var channelName = (configNames != null && ichan < configNames.Count) ? configNames[ichan] : $"CH{ichan}";

            // JSON object with clear keys
            var obj = new
            {
                file_path = FilePath,
                channel_index = ichan,
                channel_name = channelName,

                // geometry / limits
                scan_limits = new { min_scan = scanLims?[0], max_scan = scanLims?[1] },
                x_limits = new { min = xlims?[0], max = xlims?[1] },
                y_limits = new { min = ylims?[0], max = ylims?[1] },
                mps_limits = new { min = mpsLim?[0], max = mpsLim?[1] },
                tof_limits = new { min = tofLim?[0], max = tofLim?[1] },

                // 1D arrays
                tofs = tofs,
                mps = mps,
                depths = depths,
                dists = dists,
                angles = angles,
                amplitude_limits = new
                {
                    min = alims != null && alims.Length > 0 ? alims[0] : 0f,
                    max = alims != null && alims.Length > 1 ? alims[1] : 0f
                },
                indexes = indexes,
                scan_step = scanStep,

                // shapes
                sig_shape = new { beams = beams, scans = scans, depth_samples = depthSamples },

                // main data
                sig_dps = sig,          // float[beam][scan][depth]
                cscan_signal = cscanJagged
            };

            var opts = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(obj, opts);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private void SaveChannelAsCsv(int ichan, string path, char separator)
        {
            if (SigDps == null || ichan < 0 || ichan >= SigDps.Count)
                return;

            var sig = SigDps[ichan];
            int beams = sig.Length;
            if (beams == 0) return;
            int scans = sig[0].Length;
            if (scans == 0) return;
            int depthSamples = sig[0][0].Length;

            var channelName = (configNames != null && ichan < configNames.Count) ? configNames[ichan] : $"CH{ichan}";

            var sb = new StringBuilder();

            // header
            sb.Append("channel_index").Append(separator)
              .Append("channel_name").Append(separator)
              .Append("beam_index").Append(separator)
              .Append("scan_index");

            for (int d = 0; d < depthSamples; d++)
            {
                sb.Append(separator).Append("s").Append(d.ToString());
            }
            sb.AppendLine();

            // data rows: one row per (beam, scan) with full A-scan
            for (int b = 0; b < beams; b++)
            {
                for (int sIdx = 0; sIdx < scans; sIdx++)
                {
                    var row = sig[b][sIdx];
                    sb.Append(ichan).Append(separator)
                      .Append(channelName).Append(separator)
                      .Append(b).Append(separator)
                      .Append(sIdx);

                    for (int d = 0; d < depthSamples; d++)
                    {
                        sb.Append(separator)
                          .Append(row[d].ToString(CultureInfo.InvariantCulture));
                    }
                    sb.AppendLine();
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static float[][] ToJagged2D(float[,] src)
        {
            int n0 = src.GetLength(0);
            int n1 = src.GetLength(1);
            var result = new float[n0][];
            for (int i = 0; i < n0; i++)
            {
                var row = new float[n1];
                for (int j = 0; j < n1; j++)
                    row[j] = src[i, j];
                result[i] = row;
            }
            return result;
        }

        #endregion

        #region Gates
        private double[,] GatesMask { get; set; }
        private float[] _DscanMeanValues;
        public float[] DscanMeanValues
        {
            get { return _DscanMeanValues; }
            set
            {
                _DscanMeanValues = value;
                OnPropertyChanged(nameof(DscanMeanValues));
            }
        }

        private void ResetDepthLimsBasedOnGates(int ichan)
        {
            var cscan = Channels[ichan].Cscan;
            if (Application.Current.Resources["gateFirstReflection"] is string k1 &&
                cscan.Gates.TryGetValue(k1, out var t1))
                DepthMin = Math.Round(t1.Item3, 2).ToString(CultureInfo.InvariantCulture);

            if (Application.Current.Resources["gateSecondReflection"] is string k2 &&
                cscan.Gates.TryGetValue(k2, out var t2))
                DepthMax = Math.Round(t2.Item4, 2).ToString(CultureInfo.InvariantCulture);
        }


        /*
        
        public void RecalculateDscanGates(bool isSelectedChannel = true, int ichan = 0)
        {
            ichan = isSelectedChannel ? SelectedConfigIndex : ichan;
            double[,] dataDscan = Channels[ichan].Dscan.CscanData;
            float siglen = _sigDpsLengths[ichan][2];
            float mpsLen = MpsLim[ichan][1] - MpsLim[ichan][0];

            (double[,] mask, var GatesIdxs) = ComputeMaskByRowAggregate(dataDscan);
            GatesMask = mask;

            uint lineIdx = 0;
            int firstIndex_prev = 0;
            int secondIndex_prev = 0;

            var keysToRemove = Channels[ichan].Cscan.Gates.Keys.ToArray();
            Channels[ichan].Cscan.SelectedGateKey = null;
            foreach (var key in keysToRemove)
                Channels[ichan].Cscan.Gates.Remove(key);

            string gatesKey = Application.Current.Resources["gateFullDepth"] as string;
            Channels[ichan].Cscan.Gates[gatesKey] = (0, (int)siglen - 1, MpsLim[ichan][0], MpsLim[ichan][1]);

            foreach ((int start, int end) in GatesIdxs)
            {
                int firstIndex = start;
                int secondIndex = end;

                float firstIndexPRC = Math.Clamp((float)firstIndex / siglen, 0f, 1f);
                float secondIndexPRC = Math.Clamp((float)secondIndex / siglen, 0f, 1f);

                float mpsFirst = MpsLim[ichan][0] + mpsLen * firstIndexPRC;
                float mpsSecond = MpsLim[ichan][0] + mpsLen * secondIndexPRC;

                if (lineIdx == 0)
                {
                    gatesKey = Application.Current.Resources["gateWater"] as string;
                    //Channels[ichan].Cscan.Gates[gatesKey] = (0, firstIndex, MpsLim[ichan][0], mpsFirst);

                    gatesKey = Application.Current.Resources["gateFirstReflection"] as string;
                    //Channels[ichan].Cscan.Gates[gatesKey] = (firstIndex, secondIndex, mpsFirst, mpsSecond);
                }
                else
                {
                    float secondIndex_prevPRC = Math.Clamp((float)secondIndex_prev / siglen, 0f, 1f);
                    float mpsSecond_prev = MpsLim[ichan][0] + mpsLen * secondIndex_prevPRC;

                    if (lineIdx == 1)
                    {
                        gatesKey = Application.Current.Resources["gateSpecimenThickness"] as string;
                        //Channels[ichan].Cscan.Gates[gatesKey] = (secondIndex_prev, firstIndex, mpsSecond_prev, mpsFirst);

                        gatesKey = Application.Current.Resources["gateSecondReflection"] as string;
                        //Channels[ichan].Cscan.Gates[gatesKey] = (firstIndex, secondIndex, mpsFirst, mpsSecond);
                    }
                    else
                    {
                        gatesKey = Application.Current.Resources["gateBeforeReflection"] as string;
                        //Channels[ichan].Cscan.Gates[$"{gatesKey} {lineIdx}"] = (0, firstIndex, MpsLim[ichan][0], mpsFirst);

                        gatesKey = Application.Current.Resources["gateReflection"] as string;
                        //Channels[ichan].Cscan.Gates[$"{gatesKey} {lineIdx}"] = (firstIndex, secondIndex, mpsFirst, mpsSecond);
                    }
                }

                firstIndex_prev = firstIndex;
                secondIndex_prev = secondIndex;
                lineIdx++;
            }

            float secondIndex_lastPRC = Math.Clamp((float)secondIndex_prev / siglen, 0f, 1f);
            float mpsSecond_last = MpsLim[ichan][0] + mpsLen * secondIndex_lastPRC;
            //gatesKey = Application.Current.Resources["gateBelowLastReflection"] as string;
            //Channels[ichan].Cscan.Gates[gatesKey] = (secondIndex_prev, (int)siglen - 1, mpsSecond_last, MpsLim[ichan][1] - mpsLen / 100);

            //Channels[ichan].Cscan.Gates = new Dictionary<string, (int, int, float, float)>(Channels[ichan].Cscan.Gates);
            //Channels[ichan].Cscan.GatesPropertyChanged();
            //if (IsDisplayGatesMask) Channels[ichan].Dscan.AddDefectedMask(mask, ScanLims[ichan], Ylims[ichan]);

            ResetDepthLimsBasedOnGates(ichan);
        }

        */

        private bool _isBilateralFilterAscan = false;
        public bool IsBilateralFilterAscan
        {
            get => _isBilateralFilterAscan;
            set
            {
                _isBilateralFilterAscan = value;
                OnPropertyChanged(nameof(IsBilateralFilterAscan));
            }
        }

        public (double[,], List<(int, int)>) ComputeMaskByRowAggregate(double[,] inputData, bool useMedian = false)
        {
            int width = inputData.GetLength(0);
            int height = inputData.GetLength(1);

            float[] rowStat = new float[height];
            DscanMeanValues = new float[height];

            for (int y = 0; y < height; y++)
            {
                float[] rowValues = new float[width];
                for (int x = 0; x < width; x++)
                    rowValues[x] = (float)inputData[x, y];

                if (useMedian)
                {
                    var sorted = rowValues.OrderBy(v => v).ToArray();
                    int mid = width / 2;
                    if (width % 2 == 1)
                    {
                        rowStat[y] = sorted[mid];
                        DscanMeanValues[y] = rowStat[y];
                    }
                    else
                    {
                        rowStat[y] = (sorted[mid - 1] + sorted[mid]) / 2.0f;
                        DscanMeanValues[y] = rowStat[y];
                    }
                }
                else
                {
                    float sum = 0f;
                    for (int x = 0; x < width; x++)
                        sum += rowValues[x];
                    rowStat[y] = sum / width;
                    DscanMeanValues[y] = rowStat[y];
                }
            }

            var mask = new double[width, height];

            (var peaks, var deriv1, var deriv2) = FindPeaksBySecondDerivativeLogic(DscanMeanValues, IsBilateralFilterAscan);
            for (int idx = 0; idx < peaks.Count; idx++)
            {
                bool checkBoundaries = peaks[idx].Item1 > 0 && (peaks[idx].Item2 + 1) < mask.GetLength(1);
                int ymin = checkBoundaries ? peaks[idx].Item1 : 0;
                int ymax = checkBoundaries ? (peaks[idx].Item2 + 1) : mask.GetLength(1);
                for (int y = ymin; y < ymax; y++)
                    for (int x = 0; x < width; x++)
                        mask[x, y] = 500;
            }
            return (mask, peaks);
        }

        private (List<(int start, int end)>, float[], float[]) FindPeaksBySecondDerivativeLogic(float[] data, bool doFiltering = false)
        {
            float[] deriv1 = doFiltering ? ComputeGradient(BilateralFilter1D(data)) : ComputeGradient(data);
            float[] deriv2 = ComputeGradient(deriv1);
            float threshold = deriv2.Max() / 4.0f;

            List<(int start, int end)> aboveRegions = new List<(int, int)>();
            bool inRegion = false;
            int regionStart = 0;

            for (int i = 0; i < deriv2.Length; i++)
            {
                if (deriv2[i] >= threshold)
                {
                    if (!inRegion) { inRegion = true; regionStart = i; }
                }
                else
                {
                    if (inRegion)
                    {
                        aboveRegions.Add((regionStart, i - 1));
                        inRegion = false;
                    }
                }
            }
            if (inRegion) aboveRegions.Add((regionStart, deriv2.Length - 1));

            var peaks = new List<(int start, int end)>();
            for (int i = 0; i + 1 < aboveRegions.Count; i += 2)
            {
                int start = aboveRegions[i].start;
                int end = aboveRegions[i + 1].end;
                peaks.Add((start, end));
            }

            return (peaks, deriv1, deriv2);
        }
        #endregion

        #region Helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureAscending(float[] lims)
        {
            if (lims != null && lims.Length >= 2 && lims[1] < lims[0])
            {
                (lims[0], lims[1]) = (lims[1], lims[0]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double DegToRad(double deg) => deg * Math.PI / 180.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float degrees) => degrees * (float)(Math.PI / 180);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double[] Linspace(double a, double b, int n)
        {
            if (n <= 1) return new[] { a };
            double[] v = new double[n];
            double step = (b - a) / (n - 1);
            for (int i = 0; i < n; i++) v[i] = a + i * step;
            return v;
        }

        private static (double xmin, double xmax, double ymin, double ymax)
        ComputeExtents(double mpsMin, double mpsMax, float[] anglesDeg, float[] offsets)
        {
            double xmin = double.PositiveInfinity, xmax = double.NegativeInfinity;
            double ymin = double.PositiveInfinity, ymax = double.NegativeInfinity;

            for (int j = 0; j < anglesDeg.Length; j++)
            {
                double th = DegToRad(anglesDeg[j]);
                double s = Math.Sin(th);
                double c = Math.Cos(th);
                double o = offsets[j];

                double yjMin = c >= 0 ? mpsMin * c : mpsMax * c;
                double yjMax = c >= 0 ? mpsMax * c : mpsMin * c;

                double xjMin = o + (s >= 0 ? mpsMin * s : mpsMax * s);
                double xjMax = o + (s >= 0 ? mpsMax * s : mpsMin * s);

                if (xjMin < xmin) xmin = xjMin;
                if (xjMax > xmax) xmax = xjMax;
                if (yjMin < ymin) ymin = yjMin;
                if (yjMax > ymax) ymax = yjMax;
            }

            return (xmin, xmax, ymin, ymax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float[] BilateralFilter1D(float[] input, int diameter = 7, float sigmaI = 50, float sigmaS = 50)
        {
            int len = input.Length;
            float[] output = new float[len];

            for (int i = 0; i < len; i++)
            {
                float sumWeights = 0;
                float sum = 0;

                for (int j = -diameter; j <= diameter; j++)
                {
                    int idx = i + j;
                    if (idx < 0 || idx >= len)
                        continue;

                    float gi = (float)Math.Exp(-(input[idx] - input[i]) * (input[idx] - input[i]) / (2 * sigmaI * sigmaI));
                    float gs = (float)Math.Exp(-(j * j) / (2 * sigmaS * sigmaS));
                    float w = gi * gs;

                    sum += input[idx] * w;
                    sumWeights += w;
                }

                output[i] = sum / sumWeights;
            }

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float[] ComputeGradient(float[] data)
        {
            int n = data.Length;
            float[] gradient = new float[n];

            for (int i = 1; i < n - 1; i++)
                gradient[i] = (data[i + 1] - data[i - 1]) / 2.0f;

            gradient[0] = data[1] - data[0];
            gradient[n - 1] = data[n - 1] - data[n - 2];

            return gradient;
        }
        #endregion

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            public RelayCommand(Action execute) { _execute = execute; }
            public event EventHandler CanExecuteChanged;
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) { _execute(); }
        }

        public void ClearData()
        {
            Channels.Clear();
            _coordinators.Clear();

            Tofs?.Clear(); Tofs = null;
            SigDps?.Clear(); SigDps = null;
            CscanSig?.Clear(); CscanSig = null;
            Mps?.Clear(); Mps = null;
            Depths?.Clear(); Depths = null;
            Dists?.Clear(); Dists = null;

            Angles?.Clear(); Angles = null;
            Alims?.Clear(); Alims = null;
            Indexes?.Clear(); Indexes = null;
            Xlims?.Clear(); Xlims = null;
            Ylims?.Clear(); Ylims = null;
            MpsLim?.Clear(); MpsLim = null;
            TofLim?.Clear(); TofLim = null;
            _sigDpsLengths?.Clear(); _sigDpsLengths = null;
            ScanLims?.Clear(); ScanLims = null;
            ScanStep?.Clear(); ScanStep = null;

            FileInfoList?.Clear(); FileInfoList = null;
            configNames?.Clear(); configNames = null;

            FilePath = null;
            FileInfo = null;
            channels = null;
            SelectedConfigIndex = 0;
            numChannels = 0;
            numAngles = 0;
            SoundVel = 0;
            signalIndex = 10;
            scanIndex = 0;

            aiInspectionFileInfo = null;
            _ReportData = null;

            if (loadedData != null)
            {
                loadedData.ClearData();
                loadedData = null;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ===== helper types (kept in same file for convenience; move to separate files if you prefer) =====

    public sealed class ChannelContext
    {
        public int Ch { get; }
        public float[][][] SigDps { get; }
        public float[] MpsLim { get; }
        public float[] Xlims { get; }   // beams (index) world range
        public float[] Ylims { get; }   // depth world range
        public int[] ScanLims { get; }
        public float[] Alims { get; }

        public int DepthSamples { get; }
        public int Scans { get; }
        public int Beams { get; }

        public ChannelContext(int ch, float[][][] sigDps, float[] mpsLim, float[] xlims, float[] ylims, int[] scanLims, float[] alims)
        {
            Ch = ch; SigDps = sigDps; MpsLim = mpsLim; Xlims = xlims; Ylims = ylims; ScanLims = scanLims; Alims = alims;
            Beams = sigDps.Length;          // d
            Scans = sigDps[0].Length;       // s
            DepthSamples = sigDps[0][0].Length;    // b   <-- use this for Y dimension
        }
    }

    public sealed class ScanState
    {
        public int ScanIndex { get; private set; }
        public int SampleIndex { get; private set; }
        public int GateDepthMin { get; private set; }
        public int GateDepthMax { get; private set; }
        public float Gain { get; private set; } = 1f;

        public double AmpLimitMinRel { get; private set; } = 0.15;
        public double AmpLimitMaxRel { get; private set; } = 1.0;

        public event Action<int> ScanIndexChanged;
        public event Action<int> SampleIndexChanged;
        public event Action<int, int> DepthGateChanged;
        public event Action<float> GainChanged;
        public event Action<double, double> AmpLimitsChanged;

        public void SetScanIndex(int v) { if (v == ScanIndex) return; ScanIndex = v; ScanIndexChanged?.Invoke(v); }
        public void SetSampleIndex(int v) { if (v == SampleIndex) return; SampleIndex = v; SampleIndexChanged?.Invoke(v); }
        public void SetDepthGate(int g0, int g1) { if (g0 == GateDepthMin && g1 == GateDepthMax) return; GateDepthMin = g0; GateDepthMax = g1; DepthGateChanged?.Invoke(g0, g1); }
        public void SetGain(float g) { if (Math.Abs(g - Gain) < 1e-9) return; Gain = g; GainChanged?.Invoke(g); }

        public void SetAmpLimits(double minRel, double maxRel)
        {
            if (Math.Abs(minRel - AmpLimitMinRel) < 1e-12 && Math.Abs(maxRel - AmpLimitMaxRel) < 1e-12) return;
            AmpLimitMinRel = minRel; AmpLimitMaxRel = maxRel; AmpLimitsChanged?.Invoke(minRel, maxRel);
        }

    }

    public sealed class ChannelUI
    {
        public int Channel { get; init; }

        // UI controls
        public AscanPAUserControl Ascan { get; init; }
        public BscanPAUserControl Bscan { get; init; }
        public CscanPAUserControl Cscan { get; init; }
        public DscanPAUserControl Dscan { get; init; }

        // Core objects to access later
        public ChannelContext Context { get; init; }
        public ScanState State { get; init; }
        public ScanCoordinator Coordinator { get; init; }

        #region Softgan

        public ICommand RecalculateSoftGain { get; set; }

        private string _softGaindB = "0";
        public string SoftGaindB
        {
            get => _softGaindB;
            set
            {
                _softGaindB = value;
                UpdateSGdbRatio();
                OnPropertyChanged(nameof(SoftGaindB));
            }
        }
        private float _softGain = 1;
        public float SoftGain
        {
            get => _softGain;
            set
            {
                _softGain = value;
                OnPropertyChanged(nameof(SoftGain));
            }
        }

        private void UpdateSGdbRatio()
        {
            float sgdB = 0;
            var normalizedValue = SoftGaindB.Replace('.', ',');
            if (float.TryParse(SoftGaindB, out float parsedSGdB))
            {
                sgdB = parsedSGdB;
            }
            else if (float.TryParse(normalizedValue, out parsedSGdB))
            {
                sgdB = parsedSGdB;
            }

            SoftGain = (float)Math.Pow(10, sgdB / 20);
        }
        #endregion

        #region Rows Heights and Collumn width

        private GridLength _rowHeight1;
        private GridLength _rowHeight2;
        private GridLength _rowHeight3;
        private GridLength _rowHeight4;
        private GridLength _rowHeight5;
        private GridLength _rowHeight6;
        private GridLength _rowHeightBottom;

        public GridLength RowHeight1
        {
            get => _rowHeight1;
            set
            {
                _rowHeight1 = value;
                OnPropertyChanged(nameof(RowHeight1));
            }
        }

        public GridLength RowHeight2
        {
            get => _rowHeight2;
            set
            {
                _rowHeight2 = value;
                OnPropertyChanged(nameof(RowHeight2));
            }
        }

        public GridLength RowHeight3
        {
            get => _rowHeight3;
            set
            {
                _rowHeight3 = value;
                OnPropertyChanged(nameof(RowHeight3));
            }
        }

        public GridLength RowHeightBottom
        {
            get => _rowHeightBottom;
            set
            {
                _rowHeightBottom = value;
                OnPropertyChanged(nameof(RowHeightBottom));
            }
        }

        // 0 - left, 1 - splitter, 2 - right width
        private GridLength _columnWidth1;
        private GridLength _columnWidth2;
        private GridLength _columnWidth3;
        public GridLength ColumnWidth1
        {
            get => _columnWidth1;
            set
            {
                _columnWidth1 = value;
                OnPropertyChanged(nameof(ColumnWidth1));
            }
        }
        public GridLength ColumnWidth2
        {
            get => _columnWidth2;
            set
            {
                _columnWidth2 = value;
                OnPropertyChanged(nameof(ColumnWidth2));
            }
        }
        public GridLength ColumnWidth3
        {
            get => _columnWidth3;
            set
            {
                _columnWidth3 = value;
                OnPropertyChanged(nameof(ColumnWidth3));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion


    }
}
