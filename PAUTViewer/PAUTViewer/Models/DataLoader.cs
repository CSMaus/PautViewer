using System;
using System.IO;
using System.Collections.Generic;
using OlympusNDT.Storage.NET;
using System.Linq;
using Accord;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace PAUTViewer.Models
{
    public class DataLoader
    {
        public int currentBeam { get; set; }
        public string FilePath { get; set; }
        public ObservableCollection<string> FileInformation { get; set; }
        public List<string> configNames = new List<string>();
        public List<float[]> Tofs = new List<float[]>();
        // public List<float[,,]> SigDps = new List<float[,,]>();
        public List<float[][][]> SigDps = new List<float[][][]>();
        public List<float[,]> CscanSig = new List<float[,]>();
        public List<int[]> ScanLims = new List<int[]>();
        public List<float[]> Angles = new List<float[]>();
        public List<float[]> Alims = new List<float[]>();
        public List<float[]> Indexes = new List<float[]>(); // starts for A-scan on X-axis for S-scan and C-scan
        public List<float> ScanStep = new List<float>();
        public float SoundVel;
        public uint numAngles;


        // strill incorrect reading biffer keys of A-scan and C-scan for some opd files (composite plate)

        public void ReadFilePreviewInfo(string dataFilePath)
        {

        }

        private Stopwatch stopwatch = new Stopwatch();
        public void ReadDataFromFile(string dataFilePath)
        {
            configNames = new List<string>();
            Tofs = new List<float[]>();
            SigDps = new List<float[][][]>();
            CscanSig = new List<float[,]>();
            ScanLims = new List<int[]>();
            Angles = new List<float[]>();
            Alims = new List<float[]>();
            Indexes = new List<float[]>();

            FilePath = dataFilePath;
            string fileName = Path.GetFileName(dataFilePath);

            FileInformation = new ObservableCollection<string>();
            configNames = new List<string>();

            Utilities.ResolveDependenciesPath();
            string ext = System.IO.Path.GetExtension(dataFilePath);

            // TODO: need to include it in the project, not to be the local path
            string executablePath = "C:\\OlympusNDT\\OpenView SDK\\1.0\\Bin\\x64\\Debug\\ReadDataFile.NET.exe";
            // string executablePath = "C:\\OlympusNDT\\OpenView SDK\\1.0\\Tools\\DataFileConverter.exe";
            // string executablePath = "C:\\OlympusNDT\\OpenView SDK\\1.0\\Bin\\v141\\x64\\Release\\OlympusNDT.Storage.dll";




            // try to OpenDataFile but if error, then provide message box with error
            try
            {
                stopwatch.Start();  // 
                using (var dataFile = StorageSWIG.OpenDataFile(dataFilePath))
                {
                    stopwatch.Stop();
                    long elapsedTicks = stopwatch.ElapsedTicks;
                    Console.WriteLine("Elapsed ticks for var `dataFile = StorageSWIG.OpenDataFile(dataFilePath)`: " + elapsedTicks);


                    var signature = dataFile.GetFileSignature();
                    var filename = signature.GetFilename();
                    // Console.WriteLine("File signature: " + filename);
                    FileInformation.Add("File signature: " + filename);

                    var company = signature.GetCompanyName();
                    var version = signature.GetFileVersion();
                    var fileVersion = signature.GetFileVersion();
                    var libVersion = signature.GetLibraryVersion();

                    var originalFileSource = signature.GetOriginalFileSource();

                    var datetime = signature.GetDateTime();

                    var datafileCustomSections = dataFile.GetCustomSections();
                    datafileCustomSections.GetCount();

                    // Get all key information except A-scan buffer key and C-scan buffer keys
                    using (var setup = dataFile.GetSetup())
                    {
                        var setupSignature = setup.GetSignature();
                        var setupSignatureType = setupSignature.GetType();

                        var scanPlan = setup.GetScanPlan();

                        var inspDate = setupSignature.GetInspectionDate();
                        var swName = setupSignature.GetSoftwareName();
                        var swVer = setupSignature.GetSoftwareVersion();

                        // Console.WriteLine("  - Company: " + company + ", Application: " + swName + ", Version: " + swVer);
                        FileInformation.Add("  - Company: " + company + ", Application: " + swName + ", Version: " + swVer);
                        // Console.WriteLine("  - Date Time: " + inspDate + ", File Version: " + fileVersion + ", Library Version: " + libVersion);
                        FileInformation.Add("  - Date Time: " + inspDate + ", File Version: " + fileVersion + ", Library Version: " + libVersion);

                        string originalFileSourceVal = "None";
                        if (originalFileSource != null) originalFileSourceVal = originalFileSource.ToString();
                        // Console.WriteLine("  - Original File Source: " + originalFileSourceVal);
                        FileInformation.Add("  - Original File Source: " + originalFileSourceVal);

                        var specimen = scanPlan.GetSpecimen();
                        var EquipmentFactor = scanPlan.GetEquipmentFactory();
                        var inspMethodCollection = scanPlan.GetInspectionMethodCollection();

                        var specType = specimen.GetType();

                        var specTypeList = specType.GetInterfaces().ToList();
                        var specTypeMembers = specType.GetMembers().ToList();
                        var specNestedTypes = specType.GetNestedTypes().ToList();
                        var specTypeProperties = specType.GetProperties().ToList();
                        var specTypeMethods = specType.GetMethods().ToList();

                        var geometry = specimen.GetGeometry();
                        var geomMembers = geometry.GetType().GetMembers().ToList();
                        float geomLength = 0;
                        float geomWidth = 0;
                        float geomThickness = 0;

                        MethodInfo getLengthMethod = geometry.GetType().GetMethod("GetLength");
                        if (getLengthMethod != null)
                        {
                            geomLength = Convert.ToSingle(getLengthMethod.Invoke(geometry, null));
                        }
                        MethodInfo getWidthMethod = geometry.GetType().GetMethod("GetWidth");
                        if (getWidthMethod != null)
                        {
                            geomWidth = Convert.ToSingle(getWidthMethod.Invoke(geometry, null));
                        }
                        MethodInfo getThicknessMethod = geometry.GetType().GetMethod("GetThickness");
                        if (getThicknessMethod != null)
                        {
                            geomThickness = Convert.ToSingle(getThicknessMethod.Invoke(geometry, null));
                        }
                        var specTypeList0List = specTypeList[0];

                        var material = specimen.GetMaterial();
                        var geometryName = geometry.GetType().Name;

                        // take from geometryName the part after "I" and before "Geometry"
                        var geometryRealName = geometryName.Substring(1, geometryName.Length - 9);

                        var specRealName = specType.Name.Substring(1, specType.Name.Length - 9);

                        // Console.WriteLine("Specimen: " + specRealName + " " + geometryRealName + ", Length: " + geomLength + ", Width: " + geomWidth + ", Thickness: " + geomThickness);
                        FileInformation.Add("Specimen: " + specRealName + " " + geometryRealName + ", Length: " + geomLength + ", Width: " + geomWidth + ", Thickness: " + geomThickness);

                        var materialName = material.GetName();
                        var ShearVelocity = material.GetShearVelocity();
                        var longVel = material.GetLongitudinalVelocity();

                        var SpecimenPosition = scanPlan.GetSpecimenPosition();
                        var specPosAngle = SpecimenPosition.GetAngle();
                        var specPosX = SpecimenPosition.GetX();
                        var specPosY = SpecimenPosition.GetY();
                        var SpecimenPositionType = SpecimenPosition.GetType();

                        var patches = scanPlan.GetPatches();
                        uint patchCount = patches.GetCount();

                        for (uint i = 0; i < patchCount; i++)
                        {
                            var patch = patches.GetPatch(i);
                            var patchScanType = patch.GetScanType();
                            var patchScanTypeCode = patchScanType.GetTypeCode();

                            var surf = patch.GetSurface();

                            var surfType = surf.GetType();
                            var surfTypeList = surfType.GetProperties().ToList();
                            var surfTypeGUID = surfType.GUID.ToString();

                            var surTypeNested = surfType.GetNestedTypes().ToList();
                            var surTypeMethods = surfType.GetMethods().ToList();
                            var members = surfType.GetMembers().ToList();

                            var patchTypes = patch.GetType().GetProperties().ToList();
                            foreach (var patchType in patchTypes)
                            {
                                // Console.WriteLine(patchType.Name + ": " + patchType.GetValue(specType));
                                FileInformation.Add(patchType.Name + ": " + patchType.GetValue(specType));
                            }

                            var patchScanAxis = patch.GetScanAxis();
                            var patchScanAxisOrigin = Math.Round(patchScanAxis.GetOrigin(), 4);
                            var patchScanAxisLen = Math.Round(patchScanAxis.GetLength(), 3);
                            var patchScanAxisRes = Math.Round(patchScanAxis.GetResolution(), 3);
                            var saUnit = patchScanAxis.GetUnit();
                            var patchScanAxisEncoder = patchScanAxis.GetEncoder();
                            // Console.WriteLine("Scan Axis Dimension [" + "Origin: " + patchScanAxisOrigin + saUnit + ", Length: " + patchScanAxisLen + saUnit + ", Resolution: " + patchScanAxisRes + saUnit + " ]");
                            FileInformation.Add("Scan Axis Dimension [" + "Origin: " + patchScanAxisOrigin + saUnit + ", Length: " + patchScanAxisLen + saUnit + ", Resolution: " + patchScanAxisRes + saUnit + " ]");

                            var idxAxis = patch.GetIndexAxis();
                            var unit = idxAxis.GetUnit();
                            var idxAxisOrigin = Math.Round(idxAxis.GetOrigin(), 4);
                            var idxAxisLen = Math.Round(idxAxis.GetLength(), 3);
                            var idxAxisRes = Math.Round(idxAxis.GetResolution(), 3);
                            // Console.WriteLine("Index Axis Dimension [" + "Origin: " + idxAxisOrigin + unit + ", Length: " + idxAxisLen + unit + ", Resolution: " + idxAxisRes + unit + " ]");
                            FileInformation.Add("Index Axis Dimension [" + "Origin: " + idxAxisOrigin + unit + ", Length: " + idxAxisLen + unit + ", Resolution: " + idxAxisRes + unit + " ]");

                        }

                        var AcquisitionUnits = scanPlan.GetAcquisitionUnits();
                        var acqUnitType = AcquisitionUnits.GetType();
                        var acqUnitCount = AcquisitionUnits.GetCount();
                        var AcquisitionUnitsUnit = AcquisitionUnits.GetAcquisitionUnit(0);
                        var acqUnitName = AcquisitionUnitsUnit.GetName();
                        var model = AcquisitionUnitsUnit.GetModel();
                        var AcquisitionUnitsUnitPlatform = AcquisitionUnitsUnit.GetPlatform();

                        // Console.WriteLine("Acquisition Unit: " + acqUnitName + ", " + AcquisitionUnitsUnitPlatform + ", " + model);
                        FileInformation.Add("Acquisition Unit: " + acqUnitName + ", " + AcquisitionUnitsUnitPlatform + ", " + model);

                        var specGeom = scanPlan.GetSpecimen().GetGeometry(); // IPlateGeomertry - then get this as Specimen [ "Plate", 
                        var specGeomType = specGeom.GetType();
                        var specGeomPropList = specimen.GetGeometry().GetType().GetProperties().ToList();


                        var t = scanPlan.GetInspectionMethodCollection().GetInspectionMethod(0);
                        var specGeomTypeList = specGeomType.GetProperties().ToList();
                        var specGeomTypeNestedTypes = specGeomType.GetNestedTypes().ToList();

                        for (uint inspIdx = 0; inspIdx < setup.GetInspectionConfigurations().GetCount(); inspIdx++)
                        {
                            using (var inspConfig = setup.GetInspectionConfigurations().GetConfiguration(inspIdx))
                            {
                                var inspConfigAcquisitionUnits = inspConfig.GetAcquisitionUnitConfigurations().GetCount();
                                var aunitConfig = inspConfig.GetAcquisitionUnitConfigurations().GetConfiguration(0);
                                var sn = aunitConfig.GetSerialNumber();
                                var inspModel = aunitConfig.GetModel();
                                var aunitconfname = aunitConfig.GetName();
                                var aunitConfigPlatform = aunitConfig.GetPlatform();
                                var aunitUltrasoundDigitizer = aunitConfig.GetUltrasoundDigitizerConfiguration();

                                var aquisitionConfigRate = inspConfig.GetRate();
                                var firingTrigger = inspConfig.GetFiringTrigger();

                                // Console.WriteLine("Inspection configuration: " + aunitconfname + ": ");
                                FileInformation.Add("Inspection configuration: " + aunitconfname + ": ");
                                // Console.WriteLine("  - Acquisition Rate: " + aquisitionConfigRate + " Hz");
                                FileInformation.Add("  - Acquisition Rate: " + aquisitionConfigRate + " Hz");
                                // Console.WriteLine("  - Firing Trigger: " + firingTrigger);
                                FileInformation.Add("  - Firing Trigger: " + firingTrigger);


                                for (uint configIdx = 0; configIdx < inspConfig.GetConfigurations().GetCount(); configIdx++)
                                {
                                    using (var config = inspConfig.GetConfigurations().GetConfiguration(configIdx))
                                    {
                                        var convConfig = config as IConventionalConfiguration;
                                        var paConfig = config as IPhasedArrayConfiguration;

                                        var paConfigMethods = paConfig.GetType().GetMethods().ToList();
                                        var configMehods = config.GetType().GetMethods().ToList();

                                        // var convConfigCalibrationStates = convConfig.GetGateConfigurations();
                                        // var test = convConfig.GetPulsingSettings();
                                        // var test2 = convConfig.GetInspectionMethod();


                                        var inspMethod = config.GetInspectionMethod();
                                        var inspMethodName = config.GetInspectionMethod().GetName();
                                        configNames.Add(inspMethodName);

                                        var inspMethodMethods = inspMethod.GetType().GetMethods().ToList();
                                        var paInspMethod = inspMethod as IInspectionMethodPhasedArray;
                                        var convInspMethod = inspMethod as IInspectionMethodConventional;
                                        if (paConfig != null)
                                        {

                                            // take value of "beamSetFormationName" from 1st letter and split it into words. Result will be needed name

                                            // Console.WriteLine("Configuration Phased Array : " + inspMethodName);
                                            FileInformation.Add("Configuration Phased Array : " + inspMethodName);

                                            var probe = paInspMethod.GetProbe();
                                            var probeName = probe.GetName();
                                            var probePos = paInspMethod.GetProbePosition();
                                            var probePosX = probePos.GetX();
                                            var probePosY = probePos.GetY();
                                            var probePosAngle = probePos.GetAngle();

                                            // Console.WriteLine("  - Probe: " + probeName + ", Position: [ X: " + probePosX + ", Y: " + probePosY + ", Angle: " + probePosAngle + "]");
                                            FileInformation.Add("  - Probe: " + probeName + ", Position: [ X: " + probePosX + ", Y: " + probePosY + ", Angle: " + probePosAngle + "]");

                                            var b = paConfig.GetBeam(0);
                                            var paUsage = paInspMethod.GetUsage();

                                            var paBeamCount = paConfig.GetBeamCount();
                                            string beamFormation = "Unknown";
                                            if (paInspMethod != null)
                                            {
                                                var paInspMethodBeamSet = paInspMethod.GetBeamSet();
                                                var paInspMethodBeamSetFormation = paInspMethodBeamSet.GetFormation();

                                                var waveType = paInspMethodBeamSet.GetWaveType().ToString();

                                                var paFormation = paInspMethodBeamSet.GetFormation();
                                                var paFormationName = paFormation.GetType().Name;
                                                var NumberOfElementPrimaryAxis = paFormation.GetNumberOfElementPrimaryAxis();
                                                var paFormationMethods = paFormation.GetType().GetMethods().ToList();
                                                bool hasM = paFormation.HasMethod("GetAngleRefractedPrimary"); // Secondary
                                                // get methods for paFormation


                                                var beamSetFormationName = paInspMethodBeamSetFormation.GetType().Name;
                                                beamFormation = beamSetFormationName.Substring(10, beamSetFormationName.Length - 10);
                                                // this with angles range can be used only for sectorial beam formation
                                                MethodInfo getGetAngleRefractedPrimary = paFormation.GetType().GetMethod("GetAngleRefractedPrimary");
                                                if (getGetAngleRefractedPrimary != null)
                                                {
                                                    var angleRefractedPrimary = getGetAngleRefractedPrimary.Invoke(paFormation, null);
                                                    var teeee = angleRefractedPrimary.GetType().GetMethods().ToList();
                                                    float startA = 0;
                                                    float stopA = 0;
                                                    float stepA = 0;

                                                    MethodInfo getStartAngle = angleRefractedPrimary.GetType().GetMethod("GetStart");
                                                    if (getStartAngle != null)
                                                    {
                                                        startA = Convert.ToSingle(getStartAngle.Invoke(angleRefractedPrimary, null));
                                                    }
                                                    MethodInfo getStopAngle = angleRefractedPrimary.GetType().GetMethod("GetStop");
                                                    if (getStopAngle != null)
                                                    {
                                                        stopA = Convert.ToSingle(getStopAngle.Invoke(angleRefractedPrimary, null));
                                                    }
                                                    MethodInfo getStepAngle = angleRefractedPrimary.GetType().GetMethod("GetStep");
                                                    if (getStepAngle != null)
                                                    {
                                                        stepA = Convert.ToSingle(getStepAngle.Invoke(angleRefractedPrimary, null));
                                                    }
                                                }
                                            }

                                            // Console.WriteLine("Beam Formation: " + beamFormation + " with " + paBeamCount + " beams");
                                            FileInformation.Add("Beam Formation: " + beamFormation + " with " + paBeamCount + " beams");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Get info about A-scan and C-scan buffer keys info
                    using (var data = dataFile.GetData())
                    {
                        foreach (var configName in configNames)
                        {
                            // uint numAngles = 1;
                            uint lenSignal = 1;
                            uint numScanSteps = 1;
                            float tofMin = 0;
                            float tofMax = 0;
                            float tofStep = 1;

                            var ascanBufferKeys = data.GetAscanBufferKeys();
                            uint numAscanBuffers = ascanBufferKeys.GetCount();
                            var buffer0 = data.GetAscanBuffer(ascanBufferKeys.GetBufferKey(0));
                            var mergedABuffer = buffer0.GetDescriptor().IsBufferMerged();

                            string bufferName = mergedABuffer ? "Merged Ascan buffer keys of " : "Ascan buffer keys of ";
                            if (mergedABuffer)
                            {
                                var bufferICQ0 = buffer0.GetIndexCellQuantity();
                                // Console.WriteLine(bufferName + bufferICQ0 / 2 + " beams");
                                FileInformation.Add(bufferName + bufferICQ0 / 2 + " beams");
                            }
                            else
                            {
                                // Console.WriteLine(bufferName + numAscanBuffers + " beams");
                                FileInformation.Add(bufferName + numAscanBuffers + " beams");
                            }


                            // do same for Cscan buffers
                            // for C-scan buffers need to get Gates (names and num of buffers for each gate)
                            var cscanBufferKeys = data.GetCscanBufferKeys();
                            uint numCscanBuffers = cscanBufferKeys.GetCount();
                            var buffer0C = data.GetCscanBuffer(cscanBufferKeys.GetBufferKey(0));
                            var mergedCBuffer = buffer0C.GetDescriptor().IsBufferMerged();

                            var desc = buffer0C.GetDescriptor();
                            var key = desc.GetKey();
                            var key0 = cscanBufferKeys.GetBufferKey(0);
                            var key0GateName = key0.GetGateName();
                            var key0BeamSetName = key0.GetBeamSetName();

                            IDictionary<string, uint> gateBuffers = new Dictionary<string, uint>();

                            for (uint bufferIdx = 0; bufferIdx < cscanBufferKeys.GetCount(); bufferIdx++)
                            {
                                using (var buffer = data.GetCscanBuffer(cscanBufferKeys.GetBufferKey(bufferIdx)))
                                {
                                    using (var descKey = buffer.GetDescriptor().GetKey())
                                    {
                                        string gateName = descKey.GetGateName();
                                        int beamIndex = descKey.GetBeamIndex();

                                        if (!gateBuffers.ContainsKey(gateName))
                                        {
                                            uint numBuffers = 1;
                                            if (mergedCBuffer)
                                            {
                                                numBuffers = (uint)(buffer.GetIndexCellQuantity() / 2);
                                            }
                                            gateBuffers.Add(gateName, numBuffers);
                                        }
                                        else
                                        {
                                            uint numBuffers = 1;
                                            if (mergedCBuffer)
                                            {
                                                numBuffers = (uint)(buffer.GetIndexCellQuantity() / 2);
                                            }
                                            gateBuffers[gateName] += numBuffers;
                                        }
                                    }
                                }
                            }
                            var cscanBufferKeysMethods = cscanBufferKeys.GetType().GetMethods().ToList();

                            string bufferNameC = mergedCBuffer ? "Merged Cscan buffer keys: " : "Cscan buffer keys: ";

                            string cscanGatesAndBeamNum = "";
                            foreach (var gate in gateBuffers)
                            {
                                cscanGatesAndBeamNum += gate.Key + " (" + gate.Value + "), ";
                            }

                            if (mergedCBuffer)
                            {
                                // Console.WriteLine(bufferNameC + cscanGatesAndBeamNum);
                                FileInformation.Add(bufferNameC + cscanGatesAndBeamNum);
                            }
                            else
                            {
                                // Console.WriteLine(bufferNameC + cscanGatesAndBeamNum);
                                FileInformation.Add(bufferNameC + cscanGatesAndBeamNum);
                            }
                        }
                    }


                    float alimMin = 0;
                    float alimMax = 0;

                    // Read and write the data into variables
                    using (var data = dataFile.GetData())
                    {
                        int ichan = 0;
                        foreach (var configName in configNames)
                        {
                            ichan += 1;
                            // uint numAngles = 1;
                            int lenSignal = 1;
                            int numScanSteps = 1;
                            float tofMin = 0;
                            float tofMax = 0;
                            float tofStep = 1;


                            // first will take beam keys and beams
                            // this is the way to extract data from FPD files
                            // need to think about posibility to extract beam data
                            // and take a look at the values of C-scan data
                            var ascanBufferKeys = data.GetAscanBufferKeys();
                            uint numAscanBuffers = ascanBufferKeys.GetCount();
                            for (uint keyIndex = 0; keyIndex < numAscanBuffers; keyIndex++)
                            {
                                var key = ascanBufferKeys.GetBufferKey(keyIndex);
                                var ascanBufferData = data.GetAscanBuffer(key);
                                var descriptor = ascanBufferData.GetDescriptor();
                                var numIndx = ascanBufferData.GetIndexCellQuantity();
                                var numScan = ascanBufferData.GetScanCellQuantity();
                                var numSampl = ascanBufferData.GetSampleQuantity();

                                var ampAxis = descriptor.GetAmplitudeSamplingAxis();
                                var minAmp = ampAxis.GetMin();
                                var maxAmp = ampAxis.GetMax();
                                var resAmp = ampAxis.GetResolution();
                                var ampMultiplier = descriptor.GetAmplitudeMultiplierFactor();

                                var scanAxis = descriptor.GetScanAxis();
                                var minScan = scanAxis.GetMin();
                                var maxScan = scanAxis.GetMax();
                                float resScan = Convert.ToSingle(scanAxis.GetResolution());

                                if (ScanStep.Count() < ichan) ScanStep.Add(resScan);

                                var indexAxis = descriptor.GetIndexAxis();
                                var minIndex = indexAxis.GetMin();
                                var maxIndex = indexAxis.GetMax();
                                var resIndex = indexAxis.GetResolution();

                                var ultraAxis = descriptor.GetUltrasoundAxis();
                                var minUltra = ultraAxis.GetMin();
                                var maxUltra = ultraAxis.GetMax();
                                var resUltra = ultraAxis.GetResolution();

                                // var skewAngle = descriptor.GetSkewAngle();
                                var refractedAngle = descriptor.GetRefractedAnglePrimaryAxis();


                                uint rowQty = numIndx;
                                uint colQty = numScan;
                                uint sampleQty = numSampl;
                                DataType dataType = ascanBufferData.GetDataType();

                                // don't see any place with using this one
                                // float[,] sigDspTest = new float[numScan, numSampl]; 

                            }
                            /*
                             * 
                             // OLD WORKING CODE, BUT SLOW
                                List<float> maxValsTotal = new List<float>();
                                List<float> minValsTotal = new List<float>();
                                float resolutionAmplitude = 1;
                                numAngles = ascanBuffers.GetCount();
                                float[] anglesChan = new float[ascanBuffers.GetCount()];
                                float[] indexes = new float[ascanBuffers.GetCount()];

                                List<float[,,]> sigDspTests = new List<float[,,]>();

                                // trying to get more info
                                var buffer0 = ascanBuffers.GetBuffer(0);
                                var bufferDescriptor = buffer0.GetDescriptor();
                                bool isBufferMerged = bufferDescriptor.IsBufferMerged();




                                for (uint bufferIdx = 0; bufferIdx < numAngles; bufferIdx++)
                                {
                                    using (var buffer = ascanBuffers.GetBuffer(bufferIdx))
                                    {
                                        var buffMethods = buffer.GetType().GetMethods();


                                        var bufferICQ = buffer.GetIndexCellQuantity();
                                        var bufferSCQ = buffer.GetScanCellQuantity();
                                        var bufferSMQ = buffer.GetSampleQuantity();



                                        var bufferTypeProperties = buffer.GetType().GetProperties().ToList();
                                        var bufferTypeMethods = buffer.GetType().GetMethods().ToList();

                                        using (var descriptor = buffer.GetDescriptor())
                                        {
                                            using (var key = descriptor.GetKey())
                                            {
                                                int beamIndex = key.GetBeamIndex();
                                                Console.WriteLine("Beam index: " + beamIndex);
                                                currentBeam  = beamIndex;

                                                var BeamSetName = key.GetBeamSetName();

                                            }

                                            float ultrasoundSpeed = descriptor.GetUltrasoundVelocity();
                                            SoundVel = ultrasoundSpeed;
                                            float primaryAngle = descriptor.GetRefractedAnglePrimaryAxis();

                                            // float secondaryAngle = descriptor.GetRefractedAngleSecondaryAxis();
                                            // float skewAngle = descriptor.GetSkewAngle();
                                            // float amplitudeMultiplierFactor = descriptor.GetAmplitudeMultiplierFactor();

                                            //var test = descriptor.GetSkewAngle();
                                            //var refractedAngle = descriptor.GetRefractedAnglePrimaryAxis();
                                            //var refType = refractedAngle.GetType();



                                            //using (var dataAxis = descriptor.GetAmplitudeSamplingAxis())
                                            //{
                                            //    float min = dataAxis.GetMin();
                                            //    float max = dataAxis.GetMax();
                                            //    float resolution = dataAxis.GetResolution();
                                            //    DataUnit unit = dataAxis.GetUnit();
                                            //}
                                            var dataAxis = descriptor.GetAmplitudeSamplingAxis();
                                            float min = dataAxis.GetMin();
                                            alimMin = min;
                                            float max = dataAxis.GetMax();
                                            alimMax = max;
                                            float resolution = dataAxis.GetResolution();
                                            DataUnit unit = dataAxis.GetUnit();


                                            var scanAxis = descriptor.GetScanAxis();
                                            var IndexAxis = descriptor.GetIndexAxis();
                                            var UltrasoundAxis = descriptor.GetUltrasoundAxis();
                                            var AmplitudeAxis = descriptor.GetAmplitudeAxis();
                                            var AmplitudeSamplingAxis = descriptor.GetAmplitudeSamplingAxis();

                                            var minAmpSampling = AmplitudeSamplingAxis.GetMin();
                                            var maxAmpSampling = AmplitudeSamplingAxis.GetMax();
                                            var resAmpSampling = AmplitudeSamplingAxis.GetResolution();

                                            float minScan = scanAxis.GetMin();
                                            float maxScan = scanAxis.GetMax();
                                            float resolutionScan = scanAxis.GetResolution();
                                            var scanUnit = scanAxis.GetUnit();

                                            // this indicies are used to get start INDEX (X-AXIS) position of the A-scan
                                            // the end Index position is calculated from the INDEXstart + length of the A-scan and it's angle
                                            // i e calculate time, distance, and based on angle define distance, where it's ended
                                            float minIndex = IndexAxis.GetMin();
                                            float maxIndex = IndexAxis.GetMax();
                                            float resolutionIndex = IndexAxis.GetResolution();


                                            var somevar = IndexAxis.GetUnit();
                                            indexes[bufferIdx] = minIndex;
                                            if (minIndex < 0)
                                            {
                                                anglesChan[bufferIdx] = Convert.ToSingle(primaryAngle);
                                            }
                                            else
                                            {
                                                anglesChan[bufferIdx] = -Convert.ToSingle(primaryAngle);
                                            }

                                            uint lenIdxAxis = buffer.GetIndexCellQuantity();
                                            if (numAngles == 1 && lenIdxAxis > 1)
                                            {
                                                float[] indexesRows = new float[lenIdxAxis];
                                                for (uint i = 0; i < lenIdxAxis; i++)
                                                {
                                                    indexesRows[i] = minIndex + i * resolutionIndex;
                                                }
                                                Indexes.Add(indexesRows);
                                            }

                                            float minUltrasound = UltrasoundAxis.GetMin();
                                            float maxUltrasound = UltrasoundAxis.GetMax();
                                            float resolutionUltrasound = UltrasoundAxis.GetResolution();
                                            var ultraounUnit = UltrasoundAxis.GetUnit();

                                            if (tofMax == 0 && tofMin == 0)
                                            {
                                                // too store data in micro sec instead of nano sec
                                                tofMin = minUltrasound / 1000;
                                                tofMax = maxUltrasound / 1000;
                                                tofStep = resolutionUltrasound / 1000;
                                            }

                                            // need to use this amplitude
                                            float minAmplitude = AmplitudeAxis.GetMin();
                                            float maxAmplitude = AmplitudeAxis.GetMax();
                                            resolutionAmplitude = AmplitudeAxis.GetResolution();
                                            var amplitudeUnit = AmplitudeAxis.GetUnit();
                                        }

                                        List<long> maxVals = new List<long>();
                                        List<long> minVals = new List<long>();

                                        DataType dataType = buffer.GetDataType();


                                        uint rowQty = buffer.GetIndexCellQuantity();
                                        uint colQty = buffer.GetScanCellQuantity();
                                        uint sampleQty = buffer.GetSampleQuantity();

                                        float[] rowsAngles = new float[rowQty];


                                        numScanSteps = colQty;
                                        lenSignal = sampleQty;

                                        float[,,] sigDspTest = new float[rowQty, numScanSteps, lenSignal];

                                        for (uint row = 0; row < rowQty; row++)
                                        {
                                            rowsAngles[row] = anglesChan[bufferIdx];
                                            for (uint col = 0; col < colQty; col++)
                                            {
                                                using (var ascanData = buffer.Read(col, row))
                                                {
                                                    var rawData = ascanData.GetData();
                                                    var ascan = ascanData.GetType();
                                                    // var isascanDataSaturated = ascanData.IsSaturated();
                                                    // var isascanDataSyncedOnSource = ascanData.IsSyncedOnSource();

                                                    if (rawData == IntPtr.Zero)
                                                        continue;

                                                    if (dataType == DataType.SHORT)
                                                    {
                                                        List<long> castData = new List<long>();
                                                        unsafe
                                                        {
                                                            for (int d = 0; d < sampleQty; d++)
                                                            {
                                                                var value = Marshal.ReadIntPtr(rawData, d).ToInt64();
                                                                short* ptr = (short*)rawData.ToPointer();
                                                                value = ptr[d];
                                                                castData.Add(value);
                                                                sigDspTest[row, col, d] = value * resolutionAmplitude;
                                                            }
                                                        }
                                                    }
                                                    else if (dataType == DataType.USHORT)
                                                    {
                                                        List<long> castData = new List<long>();
                                                        unsafe
                                                        {
                                                            ushort* val = (ushort*)rawData;
                                                            for (int d = 0; d < sampleQty; d++)
                                                            {
                                                                var value = Marshal.ReadIntPtr(rawData, d).ToInt64();
                                                                // var value = (ushort) Marshal.ReadInt16(rawData, d);
                                                                ushort* ptr = (ushort*)rawData.ToPointer();
                                                                value = ptr[d];
                                                                castData.Add(value);
                                                                sigDspTest[row, col, d] = value * resolutionAmplitude;
                                                            }
                                                        }
                                                        maxVals.Add(castData.Max());
                                                        minVals.Add(castData.Min());
                                                    }
                                                    else if (dataType == DataType.UCHAR) // this case for FPD data files
                                                    {
                                                        unsafe
                                                        {
                                                            byte* val = (byte*)rawData;
                                                            for (int d = 0; d < sampleQty; d++)
                                                            {
                                                                // var value = Marshal.ReadIntPtr(rawData, d).ToByteArray();
                                                                // byte* ptr = (byte*)rawData.ToPointer();

                                                                // var byteVal = ptr[d];
                                                                var valueLong = (long)val[d];
                                                                // value = ptr[d];
                                                                sigDspTest[row, col, d] = valueLong * resolutionAmplitude;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        // change it - need to use faster function - take it from mainWindow - the one which uses buffers
                                        sigDspTests.Add(sigDspTest);
                                        maxValsTotal.Add(sigDspTest.Cast<float>().Max());
                                        minValsTotal.Add(sigDspTest.Cast<float>().Min());

                                        if (numAngles == 1 && rowQty > 1)
                                        {
                                            Angles.Add(rowsAngles);
                                        }
                                    }
                                }


                                int angleIdx = 0;
                                if (numAngles > 1)
                                {
                                    float[,,] sigDspChannel1 = new float[lenSignal, numAngles, numScanSteps];
                                    foreach (var angleSigDsp in sigDspTests)
                                    {
                                        for (int i = 0; i < numScanSteps; i++)
                                        {
                                            for (int j = 0; j < lenSignal; j++)
                                            {
                                                sigDspChannel1[j, angleIdx, i] = angleSigDsp[0, i, j];
                                            }
                                        }
                                        angleIdx++;
                                    }
                                    SigDps.Add(sigDspChannel1);
                                    Angles.Add(anglesChan);
                                    Indexes.Add(indexes);
                                }
                                else if (numAngles == 1 && sigDspTests[0].GetLength(0) > 1)
                                {
                                    int numRows = sigDspTests[0].GetLength(0);
                                    float[,,] sigDspChannel1 = new float[lenSignal, numRows, numScanSteps];

                                    for (int r = 0; r < numRows; r++)
                                    {
                                        for (int i = 0; i < numScanSteps; i++)
                                        {
                                            for (int j = 0; j < lenSignal; j++)
                                            {
                                                sigDspChannel1[j, r, i] = sigDspTests[0][r, i, j];
                                            }
                                        }
                                    }
                                    SigDps.Add(sigDspChannel1);
                                }


                                var totalMax = maxValsTotal.Max();
                                var totalMin = minValsTotal.Min();
                                alimMin = totalMin;
                                alimMax = totalMax;

                                Alims.Add(new float[] { alimMin, alimMax });
                                */
                            using (var ascanBuffers = data.FindAscanBuffers(configName))
                            {
                                stopwatch.Restart();


                                List<float> maxValsTotal = new List<float>();
                                List<float> minValsTotal = new List<float>();

                                float resolutionAmplitude = 1;

                                numAngles = (uint)ascanBuffers.GetCount();
                                float[] anglesChan = new float[numAngles];    // per‑angle sign‑corrected angles
                                float[] indexes = new float[numAngles];    // per‑angle start index

                                List<float[][][]> sigDspTests = new List<float[][][]>();

                                // keep “merged” flag if you need it later
                                {
                                    var tmpBuf = ascanBuffers.GetBuffer(0);
                                    bool isMerged = tmpBuf.GetDescriptor().IsBufferMerged();
                                    tmpBuf.Dispose();
                                }

                                // ---------------------------------------------------------------------------
                                for (int bufferIdx = 0; bufferIdx < numAngles; ++bufferIdx)
                                {
                                    using (var buffer = ascanBuffers.GetBuffer((uint)bufferIdx))
                                    {
                                        // descriptor (unchanged in meaning)
                                        using (var d = buffer.GetDescriptor())
                                        {
                                            using (var key = d.GetKey())
                                            {
                                                currentBeam = key.GetBeamIndex();
                                            }

                                            SoundVel = Convert.ToSingle(d.GetUltrasoundVelocity());
                                            float primaryAngle = Convert.ToSingle(d.GetRefractedAnglePrimaryAxis());

                                            var ampAxis = d.GetAmplitudeAxis();
                                            resolutionAmplitude = Convert.ToSingle(ampAxis.GetResolution());

                                            var idxAxis = d.GetIndexAxis();
                                            float minIndex = Convert.ToSingle(idxAxis.GetMin());
                                            float resIndex = Convert.ToSingle(idxAxis.GetResolution());

                                            indexes[bufferIdx] = minIndex;
                                            anglesChan[bufferIdx] = minIndex < 0 ? primaryAngle
                                                                                 : -primaryAngle;

                                            // store full row‑index array when you have 1 angle & many rows
                                            int rowsPre = (int)buffer.GetIndexCellQuantity();
                                            if (numAngles == 1 && rowsPre > 1)
                                            {
                                                float[] idxRows = new float[rowsPre];
                                                for (int i = 0; i < rowsPre; ++i)
                                                    idxRows[i] = minIndex + i * resIndex;
                                                Indexes.Add(idxRows);                // **kept**
                                            }

                                            var ampSamplingAxis = d.GetAmplitudeSamplingAxis();
                                            alimMin = Convert.ToSingle(ampSamplingAxis.GetMin());
                                            alimMax = Convert.ToSingle(ampSamplingAxis.GetMax());

                                            var ultraAxis = d.GetUltrasoundAxis();
                                            if (tofMax == 0 && tofMin == 0)
                                            {
                                                tofMin = Convert.ToSingle(ultraAxis.GetMin()) / 1000.0f;
                                                tofMax = Convert.ToSingle(ultraAxis.GetMax()) / 1000.0f;
                                                tofStep = Convert.ToSingle(ultraAxis.GetResolution()) / 1000.0f;
                                            }
                                        }

                                        // ───────── copy block (jagged only, never >2 GB) ────────────────
                                        int rows = (int)buffer.GetIndexCellQuantity();
                                        int cols = (int)buffer.GetScanCellQuantity();
                                        int samps = (int)buffer.GetSampleQuantity();

                                        float[][][] cube = new float[rows][][];
                                        for (int r = 0; r < rows; ++r)
                                        {
                                            cube[r] = new float[cols][];
                                            for (int c = 0; c < cols; ++c)
                                                cube[r][c] = new float[samps];
                                        }

                                        float gMin = float.MaxValue;
                                        float gMax = -float.MaxValue;
                                        DataType dt = buffer.GetDataType();

                                        for (int r = 0; r < rows; ++r)
                                        {
                                            for (int c = 0; c < cols; ++c)
                                                using (var a = buffer.Read((uint)c, (uint)r))
                                                {
                                                    IntPtr raw = a.GetData();
                                                    if (raw == IntPtr.Zero) continue;

                                                    unsafe
                                                    {
                                                        if (dt == DataType.SHORT)
                                                        {
                                                            short* src = (short*)raw.ToPointer();
                                                            for (int i = 0; i < samps; ++i)
                                                            {
                                                                float v = src[i] * resolutionAmplitude;
                                                                cube[r][c][i] = v;
                                                                if (v > gMax) gMax = v;
                                                                if (v < gMin) gMin = v;
                                                            }
                                                        }
                                                        else if (dt == DataType.USHORT)
                                                        {
                                                            ushort* src = (ushort*)raw.ToPointer();
                                                            for (int i = 0; i < samps; ++i)
                                                            {
                                                                float v = src[i] * resolutionAmplitude;
                                                                cube[r][c][i] = v;
                                                                if (v > gMax) gMax = v;
                                                                if (v < gMin) gMin = v;
                                                            }
                                                        }
                                                        else // UCHAR
                                                        {
                                                            byte* src = (byte*)raw.ToPointer();
                                                            for (int i = 0; i < samps; ++i)
                                                            {
                                                                float v = src[i] * resolutionAmplitude;
                                                                cube[r][c][i] = v;
                                                                if (v > gMax) gMax = v;
                                                                if (v < gMin) gMin = v;
                                                            }
                                                        }
                                                    }
                                                }
                                        }

                                        sigDspTests.Add(cube);
                                        maxValsTotal.Add(gMax);
                                        minValsTotal.Add(gMin);


                                        // per‑row angles when single‑angle / multi‑row
                                        if (numAngles == 1 && rows > 1)
                                        {
                                            float[] rowAngles = new float[rows];
                                            for (int r = 0; r < rows; ++r) rowAngles[r] = anglesChan[bufferIdx];
                                            Angles.Add(rowAngles);
                                        }
                                    }
                                }

                                // ------------- build SigDps / Angles / Indexes exactly as before ----------
                                if (numAngles > 1)                                   // many angles, 1 row
                                {
                                    // shape of every cube in sigDspTests: [1][scan][sample]
                                    int numScanSteps_i = sigDspTests[0][0].Length;
                                    int lenSignal_i = sigDspTests[0][0][0].Length;

                                    // build new cube: [angle][scan][sample]
                                    float[][][] chanCube = new float[numAngles][][];

                                    for (int a = 0; a < (int)numAngles; ++a)
                                    {
                                        chanCube[a] = new float[numScanSteps_i][];
                                        for (int s = 0; s < numScanSteps_i; ++s)
                                        {
                                            // copy row‑0 / scan‑s / all samples
                                            chanCube[a][s] = new float[lenSignal_i];
                                            Array.Copy(sigDspTests[a][0][s],   // src samples
                                                       chanCube[a][s],         // dst
                                                       lenSignal_i);
                                        }
                                    }

                                    SigDps.Add(chanCube);          // angle‑first jagged cube
                                    Angles.Add(anglesChan);
                                    Indexes.Add(indexes);          // start index per buffer
                                }
                                else if (numAngles == 1 && sigDspTests[0].Length > 1) // one angle, many rows
                                {
                                    SigDps.Add(sigDspTests[0]);
                                }

                                // global amplitude limits
                                alimMin = minValsTotal.Min();
                                alimMax = maxValsTotal.Max();
                                Alims.Add(new[] { alimMin, alimMax });


                                stopwatch.Stop();
                                long ascanBuffers_elapsedTicks = stopwatch.ElapsedTicks;
                                Console.WriteLine("");
                                Console.WriteLine($"Elapsed ticks for old non optimized Ascan buffer function is {ascanBuffers_elapsedTicks}");
                                Console.WriteLine("");
                                ascanBuffers_elapsedTicks = 0;
                                // numAngles = ascanBuffers.GetCount();


                            }



                            int numTofs = (int)Math.Round((tofMax - tofMin) / tofStep) + 1;
                            float[] tofsChan = new float[numTofs];
                            for (int i = 0; i < numTofs; i++)
                            {
                                tofsChan[i] = tofMin + i * tofStep;
                            }
                            Tofs.Add(tofsChan);

                            //Read Cscan data.
                            using (var cscanBuffers = data.FindCscanBuffers(configName))
                            {
                                int scanMin = 0;
                                int scanMax = 0;
                                var numCscanBuffers = cscanBuffers.GetCount();
                                float ampMax = 0;
                                float ampMin = 0;
                                float res = 1;
                                List<float[]> cscanSigs = new List<float[]>();
                                int numscans = 0;

                                float[] indexes = new float[numCscanBuffers];

                                var buffer0 = cscanBuffers.GetBuffer(0);
                                var bufferDescriptor = buffer0.GetDescriptor();
                                bool isBufferMerged = bufferDescriptor.IsBufferMerged();
                                // string bufferName = isBufferMerged ? "Merged Cscan buffer keys with " : "Cscan buffer keys with ";
                                // Console.WriteLine(bufferName + numCscanBuffers + " beams");


                                for (uint bufferIdx = 0; bufferIdx < cscanBuffers.GetCount(); bufferIdx++)
                                {
                                    using (var buffer = cscanBuffers.GetBuffer(bufferIdx))
                                    {
                                        var bufferICQ = buffer.GetIndexCellQuantity();
                                        var bufferSCQ = buffer.GetScanCellQuantity();

                                        string gateName = "";
                                        using (var desc = buffer.GetDescriptor())
                                        {
                                            using (var key = desc.GetKey())
                                            {
                                                int beamIndex = key.GetBeamIndex();
                                                gateName = key.GetGateName();
                                                var BeamSetName = key.GetBeamSetName();
                                                // Console.WriteLine("Beam: " + beamIndex + ", Gate: " + gateName + "\nBeamSetName: " + BeamSetName);
                                            }

                                            var AmplitudeAxis = desc.GetAmplitudeAxis();
                                            var maxAmplitude = AmplitudeAxis.GetMax();
                                            var minAmplitude = AmplitudeAxis.GetMin();
                                            float resolutionAmplitude = Convert.ToSingle(AmplitudeAxis.GetResolution());

                                            res = resolutionAmplitude;
                                            var IndexAxis = desc.GetIndexAxis();
                                            var maxIndex = IndexAxis.GetMax();
                                            var minIndex = IndexAxis.GetMin();
                                            var resolutionIndex = IndexAxis.GetResolution();
                                            indexes[bufferIdx] = Convert.ToSingle(minIndex);

                                            var ScanAxis = desc.GetScanAxis();
                                            var maxScan = ScanAxis.GetMax();
                                            var minScan = ScanAxis.GetMin();
                                            scanMin = (int)minScan;
                                            scanMax = (int)maxScan;
                                            var resolutionScan = ScanAxis.GetResolution();

                                            var KeyEx = desc.GetKeyEx();
                                            var keyextest1 = KeyEx.GetName();
                                            var keyextest2 = KeyEx.GetType();

                                            var amplitudeSampling = desc.GetAmplitudeSamplingAxis();
                                            float maxAmplitudeSampling = Convert.ToSingle(amplitudeSampling.GetMax());
                                            float minAmplitudeSampling = Convert.ToSingle(amplitudeSampling.GetMin());
                                            float resolutionAmplitudeSampling = Convert.ToSingle(amplitudeSampling.GetResolution());
                                            ampMax = maxAmplitudeSampling / resolutionAmplitudeSampling;
                                            ampMin = minAmplitudeSampling / resolutionAmplitudeSampling;


                                        }

                                        uint rowQty = buffer.GetIndexCellQuantity();
                                        uint colQty = buffer.GetScanCellQuantity();
                                        float[] cscanSig = new float[colQty];
                                        numscans = (int)colQty;
                                        float[,] cscanChannelData = new float[rowQty, colQty];

                                        for (uint row = 0; row < rowQty; row++)
                                        {
                                            for (uint col = 0; col < colQty; col++)
                                            {
                                                using (var cscanData = buffer.Read(col, row))
                                                {
                                                    if (!cscanData.HasData())
                                                        continue;


                                                    int amplitude = cscanData.GetAmplitude();
                                                    float ampPos = cscanData.GetAmplitudeTime();
                                                    var crossingTime = cscanData.GetCrossingTime();
                                                    var position = cscanData.GetPosition();
                                                    var type = cscanData.GetType();
                                                    cscanSig[col] = amplitude * res;
                                                    cscanChannelData[row, col] = amplitude * res;
                                                }
                                            }
                                            cscanSigs.Add(cscanSig);
                                        }
                                        CscanSig.Add(cscanChannelData);
                                    }
                                }
                                ScanLims.Add(new int[] { scanMin, scanMax });
                                // float[,] cscanChannelData = new float[numCscanBuffers, numscans];
                                /*
                                                                Parallel.For(0, numscans, j =>
                                                                {
                                                                    for (int i = 0; i < numCscanBuffers; i++)
                                                                    {
                                                                        cscanChannelData[i, j] = cscanSigs[i][j];
                                                                    }
                                                                });
                                */
                                // Alims.Add(new float[] { ampMin, ampMax });
                                // Indexes.Add(indexes);
                            }


                            // Read TFM data.
                            using (var tfmBuffer = data.FindTotalFocusingMethodBuffer(configName))
                            {
                                // do the same as done with C-scan but for total focusing method functions:
                                if (tfmBuffer == null)
                                    continue;
                                using (var descriptor = tfmBuffer.GetDescriptor())
                                {
                                    var AmplitudeAxis = descriptor.GetAmplitudeAxis();
                                    var maxAmplitude = AmplitudeAxis.GetMax();
                                    var minAmplitude = AmplitudeAxis.GetMin();
                                    var resolutionAmplitude = AmplitudeAxis.GetResolution();

                                    var IndexAxis = descriptor.GetIndexAxis();
                                    var maxIndex = IndexAxis.GetMax();
                                    var minIndex = IndexAxis.GetMin();
                                    var resolutionIndex = IndexAxis.GetResolution();



                                    var key = descriptor.GetKey();
                                    var columnQuantity = key.GetColumnQuantity();
                                    var description = key.GetDescription();

                                    for (uint col = 0; col < columnQuantity; col++)
                                    {
                                        using (var tfmData = tfmBuffer.Read(col, 0))
                                        {
                                            if (!tfmData.HasData())
                                                continue;

                                            // int amplitude = cscanData.GetAmplitude();
                                            // float ampPos = cscanData.GetAmplitudeTime();
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                /*var processStartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = dataFilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    var output = new StringBuilder();
                    var error = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    string capturedOutput = output.ToString();
                    string capturedError = error.ToString();
                    FileInformation.Add(capturedOutput);

                    //Console.WriteLine("Captured Output:");
                    //Console.WriteLine(capturedOutput);
                    //Console.WriteLine("Captured Error:");
                    //Console.WriteLine(capturedError);
                }
                */
            }
            catch
            {
                Console.WriteLine($"Error reading file {fileName}");
            }
            // return (FileInformation, configNames);
        }


        /*
        public void ClearData()
        {
            FileInformation.Clear();
            SigDps.Clear();
            Angles.Clear();
            Indexes.Clear();
            Tofs.Clear();
            CscanSig.Clear();
            ScanLims.Clear();
            Alims.Clear();
            ScanStep.Clear();
        }
        */
        public void ClearData()
        {
            FileInformation?.Clear();

            // Clear large 3D arrays - these are the biggest memory consumers
            if (SigDps != null)
            {
                foreach (var sigDp in SigDps)
                {
                    if (sigDp != null)
                    {
                        for (int i = 0; i < sigDp.Length; i++)
                        {
                            sigDp[i] = null;
                        }
                    }
                }
                SigDps.Clear();
            }

            // Clear 2D arrays
            if (CscanSig != null)
            {
                CscanSig.Clear();
            }

            // Clear other data arrays
            Angles?.Clear();
            Indexes?.Clear();
            Tofs?.Clear();
            ScanLims?.Clear();
            Alims?.Clear();
            ScanStep?.Clear();

            // Clear config names
            configNames?.Clear();

            // Reset basic properties
            FilePath = null;
            currentBeam = 0;
            SoundVel = 0;
            numAngles = 0;

            // Force garbage collection for large data cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }


    }
}
