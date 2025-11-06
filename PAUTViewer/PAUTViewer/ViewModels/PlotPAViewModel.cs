using OlympusNDT.Storage.NET;
using PAUTViewer.Models;
using PAUTViewer.Views;
using PAUTViewer.ProjectUtilities;
using SciChart.Charting.Visuals.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Globalization;
using System.Linq;
using ToastNotifications.Messages;

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



        // here store information with all scan plots
        // public ObservableCollection<TabContentViewModel> ChannelTabs { get; } = new ObservableCollection<TabContentViewModel>();


        private int _selectedTabIndex = -1;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged(nameof(SelectedTabIndex));
                }
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
        private int _selectedConfigIndex = 0;
        public int SelectedConfigIndex
        {
            get { return _selectedConfigIndex; }
            set
            {
                _selectedConfigIndex = value;
                OnPropertyChanged(nameof(SelectedConfigIndex));
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
            // TODO: solve the problem with error in opening files from:
            // C:\Users\Ksenia\Desktop\PAUT data\NaWoo-Data_storage\20241029-Big size scan file
            this.loadedData = loadedData;
            // AnalysisWindow = new AnalysisWindow(); // TODO:  need to close it when all others are closed. Logic should be the save ans for Dev window funciton testing
            WriteLoadedDataIntoVariables(loadedData);
            PlotData();
            // BindMenuCommandsToFunctions();

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

            Tofs = loadedData.Tofs; // time in micro sec which A-scan signal has travelled
            SigDps = loadedData.SigDps; // 3D array where [numScanSteps, numAnglesIndOneSscan, lenAscanSignal]

            // old      ------------------- float[numAngles][numScanSteps][lenSignal]
            // used here is --------------- float[lenSignal, numAngles, numScanSteps];

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

            Mps = new List<float[]>(numChannels); // can define the number of channels, but now will be like this
            Depths = new List<float[]>(numChannels);
            Dists = new List<float[]>(numChannels);

            Xlims = new List<float[]>(numChannels);
            Ylims = new List<float[]>(numChannels);
            MpsLim = new List<float[]>(numChannels);
            TofLim = new List<float[]>(numChannels);
            _sigDpsLengths = new List<int[]>(numChannels);


            aiInspectionFileInfo = new InspectionFileInfo();
            //RectangleInfos = new ObservableCollection<RectangleInfo>();
            //DefectDatas = new ObservableCollection<DefectData>();

            channels = new int[numChannels];

            for (int ichan = 0; ichan < numChannels; ichan++)
            {
                int getLen0 = SigDps[ichan].Length;
                int getLen1 = SigDps[ichan][0].Length;
                int getLen2 = SigDps[ichan][0][0].Length;

                // [numAngles][numScanSteps][lenSignal]  // new
                // [lenSignal, numAngles, numScanSteps];  // old
                _sigDpsLengths.Add(new int[] { getLen0, getLen1, getLen2 });


                numAngles = (uint)Angles[ichan].Count();
                var maxAngle = (Angles.Count() < 1) ? 0 : Angles[ichan].Max();
                channels[ichan] = ichan;
                var tofs = Tofs[ichan];
                float[] mps = tofs.Select(tof => tof / 2 * SoundVel / 1000).ToArray();
                Mps.Add(mps);

                if (numAngles == 1)
                {
                    float[] depths = mps.Select(mp => mp * (float)Math.Cos(ToRadians(maxAngle))).ToArray();  // angles[0]
                    //mps.Select(mp => mp * Math.Sin(ToRadians(angles[0]))).ToArray();
                    Depths.Add(depths);
                    Dists.Add(Indexes[ichan]);

                }
                if (numAngles > 1)  // this is the only case
                {
                    var angles = Angles[ichan];
                    var indexes = Indexes[ichan];

                    // (optional) keep your upsampling; otherwise pass angles/indexes directly
                    // float[] angles_new  = Interpolate1D(angles,  angles.Length,  upRate);
                    // float[] offset_new  = Interpolate1D(indexes, indexes.Length, upRate);
                    // var A = angles_new; var O = offset_new;

                    var A = angles;
                    var O = indexes;

                    // mps bounds
                    float mpsMin = Mps[ichan].Length > 0 ? Mps[ichan].Min() : 0f;
                    float mpsMax = Mps[ichan].Length > 0 ? Mps[ichan].Max() : 0f;

                    // O(#angles) global extents
                    var (xMin, xMax, yMin, yMax) = ComputeExtents(mpsMin, mpsMax, A, O);

                    int xCount = angles.Length;
                    int yCount = Mps[ichan].Length;

                    Depths.Add(Linspace(yMin, yMax, yCount).Select(d => (float)d).ToArray());
                    Dists.Add(Linspace(xMin, xMax, xCount).Select(d => (float)d).ToArray());

                }
                else
                {
                    // write notification?
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
                    ylims[0] = Depths[ichan][0];  // YCoords[0, 0];
                    ylims[1] = Depths[ichan][Depths[ichan].GetLength(0) - 1];
                }
                else
                {
                    ylims[0] = mps[0];
                    ylims[1] = mps[mps.Length - 1];
                }
                MpsLim.Add(new float[] { mps[0], mps[mps.Length - 1] });
                TofLim.Add(new float[] { Tofs[ichan][0], Tofs[ichan][Tofs[ichan].GetLength(0) - 1] });

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
                AscanPAUserControl ascanControl = new AscanPAUserControl();
                ascanControl.CreateAscanPlotModel(MpsLim[ichan], Alims[ichan], ichan);
                // choose middle scan
                int scanIdxA = (int)(_sigDpsLengths[ichan][1] / 2);
                int sigIdxA = (int)(_sigDpsLengths[ichan][0] / 2); // index of A-scan in S-scan
                ascanControl.UpdateAscanPlotModel(SigDps[ichan], sigIdxA, scanIdxA, MpsLim[ichan], 1);
                // ascanControl.LineMovedMin += AscanDepth_LineMovedMin;
                // ascanControl.LineMovedMax += AscanDepth_LineMovedMax;
                // MyAscanControls.Add(ascanControl);

                CscanPAUserControl cscanControl = new CscanPAUserControl();
                cscanControl.CreateScanPlotModel(ichan, ScanLims[ichan], Xlims[ichan], Alims[ichan][1], _sigDpsLengths[ichan][1], _sigDpsLengths[ichan][0], ScanStep[ichan]);
                // let's try SigDps, better for max Angle == 0
                // if isReshaped == true and Ylims, then plot is D-scan
                // otherwise, it's C-scan
                cscanControl.UpdateScanPlotModel(SigPa[ichan], ScanLims[ichan], Xlims[ichan], Alims[ichan][1], new int[] { -1, -1 }, false);
                cscanControl.LineMovedScan += CscanControl_LineMovedScan;
                cscanControl.RectangleMoved += OnSNRRectangle_Changed;
                cscanControl.GatesKeyChanged += UpdateGates_Cscan;
                // cscanControl.LineMovedIndex += CscanControlIdx_LineMoved; // it could work only for linear beam formation
                // so if we will add it, then need to be oriented by one if the line series, which is also incorrect solution

                CscanPAUserControl dscanControl = new CscanPAUserControl();
                dscanControl.CreateScanPlotModel(ichan, ScanLims[ichan], Ylims[ichan], Alims[ichan][1], ScanStep[ichan], _sigDpsLengths[ichan][1], _sigDpsLengths[ichan][2], "D");
                dscanControl.UpdateScanPlotModel(SigPa[ichan], ScanLims[ichan], Ylims[ichan], Alims[ichan][1], new int[] { -1, -1 }, true);
                dscanControl.LineMovedScan += DscanControl_LineMovedScan;
                

                BscanPAUserControl bscanControl = new BscanPAUserControl();
                bscanControl.CreateScanPlotModel(ichan, Ylims[ichan], Xlims[ichan], Alims[ichan][1], lineSeries1, lineSeries2, bPalette);
                bscanControl.UpdateScanPlotModel(SigPa[ichan], (int)ScanLims[ichan][0] + 1, Xlims[ichan], Ylims[ichan], 1);
                //bscanControl.ArrowMoved += MyBscanControl_LineMoved;


                int numBeams = _sigDpsLengths[ichan][0];
                Console.WriteLine("Writing all before tabChannel vars is okay");
                var tabContent = new TabContentViewModel
                {
                    AscanPlot = ascanControl,
                    CscanPlot = cscanControl,
                    DscanPlot = dscanControl,
                    BscanPlot = bscanControl,
                    Title = $"Channel: {configNames[ichan]}",
                    IsAscanDisplayed = true,
                    IsBscanDisplayed = true,
                    IsDscanDisplayed = true,
                    IsCscanDisplayed = true,

                    OpenLBottomScan = new RelayCommand(() => OpenNewAscanWindow(ichan)),
                    // OpenRTopScan = new RelayCommand(() => OpenNewBscanWindow(ichan)),  //sawped b and d scans
                    OpenRTopScan = new RelayCommand(() => OpenNewDscanWindow(ichan)),
                    OpenLTopScan = new RelayCommand(() => OpenNewCscanWindow(ichan)),
                    // OpenRBottomScan = new RelayCommand(() => OpenNewDscanWindow(ichan)),
                    OpenRBottomScan = new RelayCommand(() => OpenNewBscanWindow(ichan)),
                    SelectSaveSignalsPath = new RelayCommand(() => SelectSaveSignalsPath(ichan)),
                    SaveSignalCommand = new RelayCommand(() => SaveSignalAction(ichan)),  // original
                    SaveSignal2JsonCommand = new RelayCommand(() => SaveSignals2JsonAction(ichan)),  // original
                    Json_InstructionFilePath_Command = new RelayCommand(() => Json_InstructionFilePath_CommandClick(ichan)),
                    AddLimitsToInstructionFile_Command = new RelayCommand(() => AddLimitsToJson(ichan)),
                    RecalculateSoftGain = new RelayCommand(() => RecalculateSoftGain_CommandClick(ichan)),

                    // SaveSignalCommand = new RelayCommand(() => SaveSignalAction(ichan)),

                    RowHeight1 = new GridLength(3, GridUnitType.Star), // left top
                    RowHeight2 = new GridLength(15),                   // left horiz splitter
                    RowHeight3 = new GridLength(1, GridUnitType.Star), // left bottom
                    RowHeight4 = new GridLength(1, GridUnitType.Star), // right top
                    RowHeight5 = new GridLength(15),                   // right horiz splitter
                    RowHeight6 = new GridLength(1, GridUnitType.Star), // right bottom
                    RowHeightBottom = GridLength.Auto,
                    ColumnWidth1 = new GridLength(1, GridUnitType.Star), // left
                    ColumnWidth2 = new GridLength(15),                   // vert splitter
                    ColumnWidth3 = new GridLength(1, GridUnitType.Star), // right

                    // MpsMax = $"{(int)(MpsLim[ichan][1])}",
                    // MpsMin = $"{(int)MpsLim[ichan][0]}",

                    ScanMax = $"{_sigDpsLengths[ichan][1] - 1}",
                    ScanMin = $"{0}",

                    BeamMin = $"{0}",
                    BeamMax = $"{numBeams - 1}",

                    DepthMin = $"{Math.Round(MpsLim[ichan][0], 2)}",
                    DepthMax = $"{Math.Round(MpsLim[ichan][1], 2)}",

                    DesiredSignalLength = $"{_sigDpsLengths[ichan][2]}",
                    AscanStart = $"{0}",
                    AscanEnd = $"{_sigDpsLengths[ichan][0] - 1}",
                    DoPrediction = new RelayCommand(() => Predict(ichan)),
                    PredictedDefectsBySignals = new double[_sigDpsLengths[ichan][1], _sigDpsLengths[ichan][0]],
                    PredictedPositionsBySignals = new float[_sigDpsLengths[ichan][1], _sigDpsLengths[ichan][0], 2],

                    // bind the commands for swapping the plots
                    SwapTopHorizontal = new RelayCommand(() => SwapHTop(ichan)),
                    SwapBottomHorizontal = new RelayCommand(() => SwapHBottom(ichan)),
                    SwapLeftVertical = new RelayCommand(() => SwapVLeft(ichan)),
                    SwapRightVertical = new RelayCommand(() => SwapVRight(ichan)),
                    UpdateGates_Click = new RelayCommand(() => UpdateGates_ClickCommand(ichan)),

                    AddNewDefectInfo = new RelayCommand(() => AddNewDefectPositionInfo(ichan)),
                    RemoveSelectedDefectInfo = new RelayCommand(() => RemoveSelectedDefectPositionInfo(ichan)),
                    DefectsInformationPosition = new ObservableCollection<DefectPosition>(),
                    ExportRealDefectsTableIntoTxt = new RelayCommand(() => WriteRealDefectInfo(ichan)),

                    //TODO: remove this DEVELOPER feature later or remake it for users: 
                    LoadDefectsInfoFromExternalFile = new RelayCommand(() => LoadRealDefectsFromAnotherFile(ichan)),
                    SoftGain = 1,
                };
                if (toSaveSignalsPath_config != "")
                {
                    tabContent.ToSaveSignalPath = toSaveSignalsPath_config;
                }
                else if (System.IO.Directory.Exists("D:\\DataSets\\!0_0NaWooDS\\2025_DS\\WOT_JSON"))
                {
                    tabContent.ToSaveSignalPath = "D:\\DataSets\\!0_0NaWooDS\\2025_DS\\WOT_JSON";
                }

                ChannelTabs.Add(tabContent);
            }

            _selectedConfigIndex = 0;
            SelectedConfigIndex = 0;

            FillAIInspectionFileInfo();
            SelectedTabIndex = 0;

            //AddSNRDefectsIntoDevTable = new RelayCommand(() => DefineRectangularDefectLocations(0, true));
            //AddRectangularDefects_DEVELOPER_Command = new RelayCommand(() => AddRectangularDefects_DEVELOPER(0));

            //BindAllScanPlots();
            //CreateAnalysisPlot();
            //totalAreaPointNumber = ChannelTabs[0].CscanPlot.CscanData.GetLength(0) * ChannelTabs[0].CscanPlot.CscanData.GetLength(1);

            //PredictAndDisplayDefects = new RelayCommand(() => MakePredictionsForAllScanData());

            //RecalculateDscanGates_ClickCommand = new RelayCommand(() => RecalculateDscanGates());


            DscanMeanValues = new float[ChannelTabs[0].DscanPlot.CscanData.GetLength(1)];
            foreach (int ichan in channels)
            {
                RecalculateDscanGates(false, ichan);
            }
            _ReportData = new ReportData();


        }

        private void FillAIInspectionFileInfo()
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(FilePath);
            string folderPath = System.IO.Path.GetDirectoryName(FilePath);
            // remove from currentFileName last part after "_" symbol:
            string _specimenName = fileName.Contains("_") ? fileName.Substring(0, fileName.LastIndexOf("_")) : "";
            string _company = "No Company Name";
            var _dateOfInspection = DateTime.Today; // "yyyy-MM-dd" 00:00:00
            var _dateOfAnalysis = DateTime.Now;

            InspectionFileInfo inf = new InspectionFileInfo();


            inf.File = fileName;
            inf.Folder = folderPath;
            inf.SpecimenName = _specimenName;
            inf.Company = _company;
            inf.DateOfInspection = _dateOfInspection;
            inf.DateOfAnalysis = _dateOfAnalysis;
            inf.UserName = "UserName";
            inf.Password = "Password";
            inf.DBName = "Database Name";
            RowHeightExport = new GridLength(0);
            ExportOptions = new List<string>() { "Export CSV", "Export DB" };

            aiInspectionFileInfo = inf;
        }


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
            if (Application.Current.Resources["gateFirstReflection"] is string k1 &&
                ChannelTabs[ichan].CscanPlot.Gates.TryGetValue(k1, out var t1))
            {
                var mpsFirst = t1.Item3;
                ChannelTabs[ichan].DepthMin = Math.Round(mpsFirst, 2)
                                                 .ToString(CultureInfo.InvariantCulture);
            }

            if (Application.Current.Resources["gateSecondReflection"] is string k2 &&
                ChannelTabs[ichan].CscanPlot.Gates.TryGetValue(k2, out var t2))
            {
                var mpsSecond = t2.Item4;
                ChannelTabs[ichan].DepthMax = Math.Round(mpsSecond, 2)
                                                 .ToString(CultureInfo.InvariantCulture);
            }
        }


        public void RecalculateDscanGates(bool isSelectedChannel = true, int ichan = 0)
        {
            ichan = isSelectedChannel ? SelectedConfigIndex : ichan;
            double[,] dataDscan = ChannelTabs[ichan].DscanPlot.CscanData;
            float siglen = _sigDpsLengths[ichan][2];
            float mpsLen = MpsLim[ichan][1] - MpsLim[ichan][0];


            // TODO: old other Gates to remove!

            (double[,] mask, var GatesIdxs) = ComputeMaskByRowAggregate(dataDscan);
            GatesMask = mask;

            uint lineIdx = 0;
            int numberOfGateLines = GatesIdxs.Count;
            int firstIndex_prev = 0;
            int secondIndex_prev = 0;

            // var keysToRemove = ChannelTabs[ichan].CscanPlot.Gates.Keys; // .Where(k => k.StartsWith("Above Line ") || k.StartsWith("Line ") || k.StartsWith("Below Line ")).ToList();
            var keysToRemove = ChannelTabs[ichan].CscanPlot.Gates.Keys.ToArray();
            Console.WriteLine($"Number Keys to remove are: {keysToRemove.Count()}");

            ChannelTabs[ichan].CscanPlot.SelectedGateKey = null;
            foreach (var key in keysToRemove)
            {
                ChannelTabs[ichan].CscanPlot.Gates.Remove(key);
            }

            string gatesKey = Application.Current.Resources["gateFullDepth"] as string;
            ChannelTabs[ichan].CscanPlot.Gates[gatesKey] = (0, (int)siglen - 1, MpsLim[ichan][0], MpsLim[ichan][1]);
            Console.WriteLine($"Number of Lines is {GatesIdxs.Count}");
            foreach ((int start, int end) in GatesIdxs)
            {
                int firstIndex = start;
                int secondIndex = end;
                if (lineIdx == 0)
                {
                    firstIndex_prev = firstIndex;
                    secondIndex_prev = secondIndex;

                    float firstIndexPRC = (float)firstIndex / siglen;
                    float secondIndexPRC = (float)secondIndex / siglen;
                    firstIndexPRC = (firstIndexPRC >= 0 && firstIndexPRC < 1) ? firstIndexPRC : 0;
                    secondIndexPRC = (secondIndexPRC > 0 && secondIndexPRC <= 1) ? secondIndexPRC : 1;
                    float mpsFirst = MpsLim[ichan][0] + mpsLen * firstIndexPRC;
                    float mpsSecond = MpsLim[ichan][0] + mpsLen * secondIndexPRC;

                    // first two values - min and max indexes of depth axis - i e indexes of points of the A-scan signal
                    // last two values are min and max Depth values based on real limits received from file info
                    // ChannelTabs[ichan].CscanPlot.Gates[$"Above Line 0"] = (0, firstIndex, MpsLim[ichan][0], mpsFirst);
                    // ChannelTabs[ichan].CscanPlot.Gates[$"Line 0"] = (firstIndex, secondIndex, mpsFirst, mpsSecond);
                    gatesKey = Application.Current.Resources["gateWater"] as string;
                    ChannelTabs[ichan].CscanPlot.Gates[gatesKey] = (0, firstIndex, MpsLim[ichan][0], mpsFirst);
                    gatesKey = Application.Current.Resources["gateFirstReflection"] as string;
                    ChannelTabs[ichan].CscanPlot.Gates[gatesKey] = (firstIndex, secondIndex, mpsFirst, mpsSecond);

                    Console.WriteLine($"Added gates for Line 0 and above");
                }
                else
                {
                    float firstIndexPRC = (float)firstIndex / siglen;
                    float secondIndexPRC = (float)secondIndex / siglen;
                    float secondIndex_prevPRC = (float)secondIndex_prev / siglen;
                    firstIndexPRC = (firstIndexPRC >= 0 && firstIndexPRC < 1) ? firstIndexPRC : 0;
                    secondIndexPRC = (secondIndexPRC > 0 && secondIndexPRC <= 1) ? secondIndexPRC : 1;
                    secondIndex_prevPRC = (secondIndex_prevPRC > 0 && secondIndex_prevPRC <= 1) ? secondIndex_prevPRC : 1;
                    float mpsFirst = MpsLim[ichan][0] + mpsLen * firstIndexPRC;
                    float mpsSecond = MpsLim[ichan][0] + mpsLen * secondIndexPRC;
                    float mpsSecond_prev = MpsLim[ichan][0] + mpsLen * secondIndex_prevPRC;

                    if (lineIdx == 1)
                    {
                        gatesKey = Application.Current.Resources["gateSpecimenThickness"] as string;
                        ChannelTabs[ichan].CscanPlot.Gates[gatesKey] = (secondIndex_prev, firstIndex, mpsSecond_prev, mpsFirst);
                        gatesKey = Application.Current.Resources["gateSecondReflection"] as string;
                        ChannelTabs[ichan].CscanPlot.Gates[gatesKey] = (firstIndex, secondIndex, mpsFirst, mpsSecond);
                    }
                    else
                    {
                        gatesKey = Application.Current.Resources["gateBeforeReflection"] as string;
                        ChannelTabs[ichan].CscanPlot.Gates[$"{gatesKey} {lineIdx}"] = (0, firstIndex, MpsLim[ichan][0], mpsFirst);
                        gatesKey = Application.Current.Resources["gateReflection"] as string;
                        ChannelTabs[ichan].CscanPlot.Gates[$"{gatesKey} {lineIdx}"] = (firstIndex, secondIndex, mpsFirst, mpsSecond);
                    }


                    firstIndex_prev = firstIndex;
                    secondIndex_prev = secondIndex;

                    Console.WriteLine($"Added gates for Line {lineIdx} and Below Line {lineIdx}");
                }
                lineIdx++;
            }

            float secondIndex_lastPRC = (float)secondIndex_prev / siglen;
            secondIndex_lastPRC = (secondIndex_lastPRC > 0 && secondIndex_lastPRC <= 1) ? secondIndex_lastPRC : 1;
            float mpsSecond_last = MpsLim[ichan][0] + mpsLen * secondIndex_lastPRC;
            gatesKey = Application.Current.Resources["gateBelowLastReflection"] as string;
            ChannelTabs[ichan].CscanPlot.Gates[gatesKey] = (secondIndex_prev, (int)siglen - 1, mpsSecond_last, MpsLim[ichan][1] - mpsLen / 100);

            ChannelTabs[ichan].CscanPlot.Gates = new Dictionary<string, (int, int, float, float)>(ChannelTabs[ichan].CscanPlot.Gates);
            ChannelTabs[ichan].CscanPlot.GatesPropertyChanged();
            if (IsDisplayGatesMask) ChannelTabs[ichan].DscanPlot.AddDefectedMask(mask, ScanLims[ichan], Ylims[ichan]);

            ResetDepthLimsBasedOnGates(ichan);
        }

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
            // calculated accumulated signal: from 2D get 1D signal
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
                        rowStat[y] = (sorted[mid - 1] + sorted[mid]) / (float)2.0;
                        DscanMeanValues[y] = rowStat[y];
                    }
                }
                else
                {
                    float sum = (float)0.0;
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
                {
                    for (int x = 0; x < width; x++)
                    {
                        mask[x, y] = 500;
                    }
                }
            }
            return (mask, peaks);
        }


        private (List<(int start, int end)>, float[], float[]) FindPeaksBySecondDerivativeLogic(float[] data, bool doFiltering = false)
        {
            // float[] bilateralData = BilateralFilter1D(data);
            float[] deriv1 = doFiltering ? ComputeGradient(BilateralFilter1D(data)) : ComputeGradient(data);
            float[] deriv2 = ComputeGradient(deriv1);
            float threshold = deriv2.Max() / (float)4.0;

            List<(int start, int end)> aboveRegions = new List<(int, int)>();
            bool inRegion = false;
            int regionStart = 0;

            for (int i = 0; i < deriv2.Length; i++)
            {
                if (deriv2[i] >= threshold)
                {
                    if (!inRegion)
                    {
                        inRegion = true;
                        regionStart = i;
                    }
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
            if (inRegion)
                aboveRegions.Add((regionStart, deriv2.Length - 1));

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
        private static double DegToRad(double deg) => deg * Math.PI / 180.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float degrees)
        {
            return degrees * (float)(Math.PI / 180);
        }

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

                // For y = mps * c
                double yjMin = c >= 0 ? mpsMin * c : mpsMax * c;
                double yjMax = c >= 0 ? mpsMax * c : mpsMin * c;

                // For x = o + mps * s
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
        private static float[] ComputeGradient(float[] data)
        {
            int n = data.Length;
            float[] gradient = new float[n];

            for (int i = 1; i < n - 1; i++)
            {
                gradient[i] = (data[i + 1] - data[i - 1]) / 2.0f; // central difference
            }

            // Edge cases: forward/backward difference
            gradient[0] = data[1] - data[0];
            gradient[n - 1] = data[n - 1] - data[n - 2];

            return gradient;
        }
        #endregion


        public class RelayCommand : ICommand
        {
            private readonly Action _execute;

            public RelayCommand(Action execute)
            {
                _execute = execute;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                _execute();
            }
        }

        public void ClearData()
        {

            //if (_analysisWindow != null && _analysisWindow.IsLoaded) _analysisWindow.Close();
            //if (_aiDefectDetectionWindow != null && _aiDefectDetectionWindow.IsLoaded) _aiDefectDetectionWindow.Close();
            //if (_dataDevTestWindow != null && _dataDevTestWindow.IsLoaded) _dataDevTestWindow.Close();
            //if (_fileInfoWindow != null && _fileInfoWindow.IsLoaded) _fileInfoWindow.Close();

            //_analysisWindow = null;
            //_aiDefectDetectionWindow = null;
            //_dataDevTestWindow = null;
            //_fileInfoWindow = null;

            //// Clear user control references
            //AscanUserControl = null;
            //BscanUserControl = null;
            //CscanUserControl = null;
            //DscanUserControl = null;

            // Clear large data collections - these are the main memory consumers
            Tofs?.Clear();
            Tofs = null;

            SigDps?.Clear();
            SigDps = null;

            CscanSig?.Clear();
            CscanSig = null;

            Mps?.Clear();
            Mps = null;

            Depths?.Clear();
            Depths = null;

            Dists?.Clear();
            Dists = null;


            Angles?.Clear();
            Angles = null;

            Alims?.Clear();
            Alims = null;

            Indexes?.Clear();
            Indexes = null;

            Xlims?.Clear();
            Xlims = null;

            Ylims?.Clear();
            Ylims = null;

            MpsLim?.Clear();
            MpsLim = null;

            TofLim?.Clear();
            TofLim = null;

            _sigDpsLengths?.Clear();
            _sigDpsLengths = null;

            ScanLims?.Clear();
            ScanLims = null;

            ScanStep?.Clear();
            ScanStep = null;


            // Clear analysis and defect data
            //RectangleInfos?.Clear();
            //RectangleInfos = null;

            //DefectDatas?.Clear();
            //DefectDatas = null;

            // Clear file information
            FileInfoList?.Clear();
            FileInfoList = null;

            configNames?.Clear();
            configNames = null;

            // Clear other collections and dictionaries
            //Entries?.Clear();
            //Entries = new Dictionary<string, Dictionary<string, float>>();

            // Clear channel tabs
            //ChannelTabs.Clear();

            // Reset basic properties
            FilePath = null;
            FileInfo = null;
            channels = null;
            SelectedConfigIndex = 0;
            numChannels = 0;
            numAngles = 0;
            SoundVel = 0;
            signalIndex = 10;
            scanIndex = 0;

            // Clear inspection info
            aiInspectionFileInfo = null;
            _ReportData = null;

            if (loadedData != null)
            {
                loadedData.ClearData(); // Will need to add this method to DataLoader
                loadedData = null;
            }

            // Force garbage collection to free memory immediately
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
