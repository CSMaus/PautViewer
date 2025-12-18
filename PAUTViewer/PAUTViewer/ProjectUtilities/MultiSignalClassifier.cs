using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ToastNotifications.Messages;

namespace PAUTViewer.ProjectUtilities
{
    public static class MultiSignalClassifier
    {
        public static float[] Predict(string modelPath, float[,] data)
        {
            int numSignals = data.GetLength(0);
            var res = new float[numSignals];

            var tempModelPath = Path.GetTempFileName();
            InferenceSession session;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // var resourceName = "PAUTReader.ProjectUtilities.MultiSignalClassifier4_dynamic.onnx";
                var resourceName = "PAUTReader.Resources.test-FPD.onnx";
                // var resourceName = "PAUTReader.ProjectUtilities.MSC_modelConv1d_OPD.onnx";
                // var resourceName = "PAUTReader.ProjectUtilities.MultiSignalClassifier4_modelOPD.onnx";
                var resourceNames = assembly.GetManifestResourceNames();
                // string logFilePath = Path.Combine("D:\\ML_DL_AI_stuff\\!VS_projcts\\PAUT_data_Analysis-SDK\\SetupProjectPAUTReader\\bin\\Release", "log.txt");
                /*
                  using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] Embedded Resources:");
                    foreach (var name in resourceNames)
                    {
                        writer.WriteLine(name);
                    }
                    writer.WriteLine();
                }
                 */



                using (var modelStream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (var fileStream = File.Create(tempModelPath))
                    {
                        modelStream.CopyTo(fileStream);
                    }
                }

                session = new InferenceSession(File.ReadAllBytes(tempModelPath));
            }
            catch (Exception ex)
            {
                string notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModel"] as string, ex.Message);
                NotificationManager.Notifier.ShowWarning(notificationText);
                try
                {
                    session = new InferenceSession(modelPath);
                }
                catch (Exception ex2)
                {
                    notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModelPath"] as string, ex2.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    File.Delete(tempModelPath);
                    return res;
                }
            }


            var modelInputLayerName = session.InputMetadata.Keys.Single();

            // Create input tensor from float[,] data (shape should be [batch_size, num_signals, signal_length])
            int signalLength = data.GetLength(1);



            DenseTensor<float> inputTensor = new DenseTensor<float>(new[] { 1, numSignals, signalLength });

            for (int i = 0; i < numSignals; i++)
            {
                for (int j = 0; j < signalLength; j++)
                {
                    inputTensor[0, i, j] = (float)data[i, j];
                }
            }
            /*
             * 
            this is for the new one - Conv1D
            DenseTensor<float> inputTensor = new DenseTensor<float>(new[] { 1, signalLength, numSignals });

            for (int i = 0; i < numSignals; i++)
            {
                for (int j = 0; j < signalLength; j++)
                {
                    inputTensor[0, j, i] = (float)data[i, j];
                }
            }
            */

            var modelInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(modelInputLayerName, inputTensor)
            };

            // Use IDisposableReadOnlyCollection for result type
            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = session.Run(modelInput))
            {
                try
                {
                    // Extract the results and return the correct size array (one result per signal)
                    var outputTensor = (DenseTensor<float>)result.Single().Value;
                    return ProcessTensor(outputTensor.ToArray());
                }
                catch (Exception ex)
                {
                    string notificationText = string.Format(Application.Current.Resources["notificationErrorModelInference"] as string, ex.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    return res;
                }
                finally
                {
                    File.Delete(tempModelPath);
                }
            }

        }
        public static (float[], float[], float[]) PredictOPD(string modelPath, string resourceName, float[,] data)
        {
            int numSignals = data.GetLength(0);
            var res = new float[numSignals];

            var tempModelPath = Path.GetTempFileName();
            InferenceSession session;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // var resourceName = "PAUTReader.Resources.002-ImprovedMSC.onnx";
                // var resourceName = "PAUTReader.Resources.000-EnhancedPositionMSC.onnx";
                // var resourceName = "PAUTReader.Resources.000-FixedEnhancedPositionMSC.onnx";
                var resourceNames = assembly.GetManifestResourceNames();


                using (var modelStream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (var fileStream = File.Create(tempModelPath))
                    {
                        modelStream.CopyTo(fileStream);
                    }
                }

                session = new InferenceSession(File.ReadAllBytes(tempModelPath));
            }
            catch (Exception ex)
            {
                string notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModel"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                try
                {
                    session = new InferenceSession(modelPath);
                }
                catch (Exception ex2)
                {
                    notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModelPath"] as string, ex2.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    File.Delete(tempModelPath);
                    return (res, res, res);
                }
            }

            var modelInputLayerName = session.InputMetadata.Keys.Single();

            // Create input tensor from float[,] data (shape should be [batch_size, num_signals, signal_length])
            int signalLength = data.GetLength(1);

            DenseTensor<float> inputTensor = new DenseTensor<float>(new[] { 1, numSignals, signalLength });

            for (int i = 0; i < numSignals; i++)
            {
                for (int j = 0; j < signalLength; j++)
                {
                    inputTensor[0, i, j] = (float)data[i, j];
                }
            }

            var modelInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(modelInputLayerName, inputTensor)
            };

            // Use IDisposableReadOnlyCollection for result type
            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = session.Run(modelInput))
            {
                try
                {
                    // Extract the results and return the correct size array (should be three results per signal: probability, defect start, defect end)

                    var outputTensors = result.ToList();
                    if (outputTensors.Count != 3)
                    {
                        string notificationText = string.Format(Application.Current.Resources["notificationUnexpectedOutputs"] as string, outputTensors.Count);
                        NotificationManager.Notifier.ShowError(notificationText);
                    }
                    var defectProbTensor = (DenseTensor<float>)outputTensors[0].Value;
                    var defectStartTensor = (DenseTensor<float>)outputTensors[1].Value;
                    var defectEndTensor = (DenseTensor<float>)outputTensors[2].Value;

                    float[] defectProbs = ProcessTensor(defectProbTensor.ToArray());
                    float[] defectStarts = defectStartTensor.ToArray();
                    float[] defectEnds = defectEndTensor.ToArray();


                    // return ProcessTensor(outputTensor.ToArray());
                    return (defectProbs, defectStarts, defectEnds);
                }
                catch (Exception ex)
                {
                    string notificationText = string.Format(Application.Current.Resources["notificationErrorModelInference"] as string, ex.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    return (res, res, res);
                }
                finally
                {
                    File.Delete(tempModelPath);
                }
            }

        }

        public static float[] PredictDetectionOriginal(string resourceName, float[,] data)
        {
            int numSignals = data.GetLength(0);
            var res = new float[numSignals];

            var tempModelPath = Path.GetTempFileName();
            InferenceSession session;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // var resourceName = "PAUTReader.Resources.000-ComplexDetectionModel.onnx";
                // var resourceName = "PAUTReader.Resources.002-ComplexDetectionModel_v3.onnx";
                // var resourceName = "PAUTReader.Resources.003-ComplexDetectionModel_v3.onnx";
                // var resourceName = "PAUTReader.Resources.0004-NoiseRobust_v3.onnx";
                // var resourceName = "PAUTReader.Resources.003-ComplexDetectionModel_v3.onnx";

                // var resourceName = "PAUTReader.Resources.006-DirectDefectModel_v3.onnx";

                // var resourceName = "PAUTReader.Resources.007-DirectDefectModel_v3.onnx";
                // var resourceName = "PAUTReader.Resources.008-DirectDefectModel_v3.onnx";
                // var resourceName = "PAUTReader.Resources.009-DirectDefectModel_v3.onnx";
                // var resourceName = "PAUTReader.Resources.011-DirectDefectModel_v3.onnx";
                // var resourceName = "PAUTReader.Resources.000-HybridBinaryModel.onnx";
                var resourceNames = assembly.GetManifestResourceNames();

                using (var modelStream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (var fileStream = File.Create(tempModelPath))
                    {
                        modelStream.CopyTo(fileStream);
                    }
                }

                session = new InferenceSession(File.ReadAllBytes(tempModelPath));
            }
            catch (Exception ex)
            {
                string notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModel"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                try
                {
                    string modelPath = @"D:\ML_DL_AI_stuff\!!NaWoo\model.onnx";

                    session = new InferenceSession(modelPath);
                }
                catch (Exception ex2)
                {
                    notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModelPath"] as string, ex2.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    File.Delete(tempModelPath);
                    return res;
                }
            }

            var modelInputLayerName = session.InputMetadata.Keys.Single();

            // Create input tensor from float[,] data (shape should be [batch_size, num_signals, signal_length])
            int signalLength = data.GetLength(1);

            DenseTensor<float> inputTensor = new DenseTensor<float>(new[] { 1, numSignals, signalLength });

            for (int i = 0; i < numSignals; i++)
            {
                for (int j = 0; j < signalLength; j++)
                {
                    inputTensor[0, i, j] = (float)data[i, j];
                }
            }

            var modelInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(modelInputLayerName, inputTensor)
            };

            // Use IDisposableReadOnlyCollection for result type
            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = session.Run(modelInput))
            {
                try
                {
                    // Extract the single detection probability output
                    var outputTensors = result.ToList();
                    if (outputTensors.Count != 1)
                    {
                        string notificationText = string.Format(Application.Current.Resources["notificationUnexpectedOutputs"] as string, outputTensors.Count);
                        NotificationManager.Notifier.ShowError(notificationText);
                    }

                    var detectionProbTensor = (DenseTensor<float>)outputTensors[0].Value;
                    float[] detectionProbs = ProcessTensor(detectionProbTensor.ToArray());

                    return detectionProbs;
                }
                catch (Exception ex)
                {
                    string notificationText = string.Format(Application.Current.Resources["notificationErrorModelInference"] as string, ex.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    return res;
                }
                finally
                {
                    File.Delete(tempModelPath);
                }
            }
        }
        public static (float[], float[]) PredictPosition(string resourceName, float[,] data)
        {
            int numSignals = data.GetLength(0);
            var res = (new float[numSignals], new float[numSignals]);

            var tempModelPath = Path.GetTempFileName();
            InferenceSession session;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // resourceName = "PAUTReader.Resources.002-ImprovedMSC.onnx";
                // var resourceName = "PAUTReader.Resources.000-EnhancedPositionMSC.onnx";
                // var resourceName = "PAUTReader.Resources.000-FixedEnhancedPositionMSC.onnx";
                var resourceNames = assembly.GetManifestResourceNames();


                using (var modelStream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (var fileStream = File.Create(tempModelPath))
                    {
                        modelStream.CopyTo(fileStream);
                    }
                }

                session = new InferenceSession(File.ReadAllBytes(tempModelPath));
            }
            catch (Exception ex)
            {
                string notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModel"] as string, ex.Message);
                NotificationManager.Notifier.ShowError(notificationText);
                try
                {
                    string modelPath = "";
                    session = new InferenceSession(modelPath);
                }
                catch (Exception ex2)
                {
                    notificationText = string.Format(Application.Current.Resources["notificationErrorLoadingONNXModelPath"] as string, ex2.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    File.Delete(tempModelPath);
                    return res;
                }
            }

            var modelInputLayerName = session.InputMetadata.Keys.Single();

            int signalLength = data.GetLength(1);

            DenseTensor<float> inputTensor = new DenseTensor<float>(new[] { 1, numSignals, signalLength });

            for (int i = 0; i < numSignals; i++)
            {
                for (int j = 0; j < signalLength; j++)
                {
                    inputTensor[0, i, j] = (float)data[i, j];
                }
            }

            var modelInput = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(modelInputLayerName, inputTensor)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = session.Run(modelInput))
            {
                try
                {

                    var outputTensors = result.ToList();
                    // if (outputTensors.Count != 2)
                    // {
                    //     string notificationText = string.Format(Application.Current.Resources["notificationUnexpectedOutputs"] as string, outputTensors.Count);
                    //     NotificationManager.Notifier.ShowError(notificationText);
                    // }

                    // use only positions from this model
                    var defectStartTensor = (DenseTensor<float>)outputTensors[1].Value;
                    var defectEndTensor = (DenseTensor<float>)outputTensors[2].Value;

                    float[] defectStarts = defectStartTensor.ToArray();
                    float[] defectEnds = defectEndTensor.ToArray();


                    // return ProcessTensor(outputTensor.ToArray());
                    return (defectStarts, defectEnds);
                }
                catch (Exception ex)
                {
                    string notificationText = string.Format(Application.Current.Resources["notificationErrorModelInference"] as string, ex.Message);
                    NotificationManager.Notifier.ShowError(notificationText);
                    return res;
                }
                finally
                {
                    File.Delete(tempModelPath);
                }
            }

        }
        public static float[] ProcessTensor(float[] inputArray, bool isReversed = false)
        {
            if (isReversed)
            {
                return inputArray.Select(value => (float)Math.Round(100 - value * 100, 2)).ToArray();
            }
            else
            {
                return inputArray.Select(value => (float)Math.Round(value * 100, 2)).ToArray();
            }
        }

        // batched predictions call
        /*
        private static InferenceSession _detSession;
        private static string _detKey;
        public static void InitDetectionSession(string resourceName)
        {
            if (_detSession != null && _detKey == resourceName) return;

            Stream s = null;
            MemoryStream ms = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                s = asm.GetManifestResourceStream(resourceName);

                if (_detSession != null) { _detSession.Dispose(); _detSession = null; }

                if (s != null)
                {
                    ms = new MemoryStream();
                    s.CopyTo(ms);
                    _detSession = new InferenceSession(ms.ToArray());
                }
                else
                {
                    _detSession = new InferenceSession(resourceName);
                }
                _detKey = resourceName;
            }
            finally
            {
                if (ms != null) ms.Dispose();
                if (s != null) s.Dispose();
            }
        }


        // New: batched inference, input shape [B, numSeq, sigLen], output -> [B, numSeq] (flattened if model returns [B,numSeq,1])
        public static float[,] PredictDetectionBatch(string resourceName, float[,,] data)
        {
            InitDetectionSession(resourceName);

            int B = data.GetLength(0);
            int N = data.GetLength(1);
            int L = data.GetLength(2);

            var input = new DenseTensor<float>(new[] { B, N, L });
            for (int b = 0; b < B; b++)
                for (int i = 0; i < N; i++)
                    for (int j = 0; j < L; j++)
                        input[b, i, j] = (float)data[b, i, j];

            string inputName = null;
            foreach (var k in _detSession.InputMetadata.Keys) { inputName = k; break; }

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = null;
            try
            {
                result = _detSession.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, input) });

                var it = result.GetEnumerator();
                if (!it.MoveNext()) throw new InvalidOperationException("No outputs.");
                var t = (DenseTensor<float>)it.Current.Value;

                if (t.Dimensions.Length == 3 && t.Dimensions[2] == 1)
                {
                    var y = new float[t.Dimensions[0], t.Dimensions[1]];
                    var arr = t.ToArray();
                    int k = 0;
                    for (int b = 0; b < y.GetLength(0); b++)
                        for (int i = 0; i < y.GetLength(1); i++, k++)
                            y[b, i] = arr[k];
                    return y;
                }
                if (t.Dimensions.Length == 2)
                {
                    var y = new float[t.Dimensions[0], t.Dimensions[1]];
                    var arr = t.ToArray();
                    int k = 0;
                    for (int b = 0; b < y.GetLength(0); b++)
                        for (int i = 0; i < y.GetLength(1); i++, k++)
                            y[b, i] = arr[k];
                    return y;
                }
                throw new InvalidOperationException("Unexpected output shape.");
            }
            finally
            {
                if (result != null) result.Dispose();
            }
        }

        // Backward-compatible wrapper (batch = 1)
        public static float[] PredictDetection(string resourceName, float[,] data)
        {
            int N = data.GetLength(0), L = data.GetLength(1);
            var x = new float[1, N, L];
            for (int i = 0; i < N; i++)
                for (int j = 0; j < L; j++)
                    x[0, i, j] = data[i, j];

            var y = PredictDetectionBatch(resourceName, x);
            var res = new float[N];
            for (int i = 0; i < N; i++) res[i] = y[0, i];
            return res;
        }
        */

        // ---- DETECTION (cached, 2D wrapper, same 0..100 scaling) ----
        private static InferenceSession _detSession;
        private static string _detKey;
        private static string _detInputName;

        public static void InitDetectionSession(string resourceName)
        {
            if (_detSession != null && _detKey == resourceName) return;

            Stream s = null; MemoryStream ms = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                s = asm.GetManifestResourceStream(resourceName);

                if (_detSession != null) { _detSession.Dispose(); _detSession = null; }

                if (s != null)
                {
                    ms = new MemoryStream();
                    s.CopyTo(ms);
                    _detSession = new InferenceSession(ms.ToArray());
                }
                else
                {
                    _detSession = new InferenceSession(resourceName);
                }
                foreach (string k in _detSession.InputMetadata.Keys) { _detInputName = k; break; }
                _detKey = resourceName;
            }
            finally
            {
                if (ms != null) ms.Dispose();
                if (s != null) s.Dispose();
            }
        }

        public static float[] PredictDetectionCached2D(string resourceName, float[,] data)
        {
            InitDetectionSession(resourceName);

            int N = data.GetLength(0);
            int L = data.GetLength(1);

            DenseTensor<float> input = new DenseTensor<float>(new int[] { 1, N, L });
            int i, j;
            for (i = 0; i < N; i++)
                for (j = 0; j < L; j++)
                    input[0, i, j] = (float)data[i, j];

            List<NamedOnnxValue> feeds = new List<NamedOnnxValue>(1);
            feeds.Add(NamedOnnxValue.CreateFromTensor(_detInputName, input));

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = null;
            try
            {
                result = _detSession.Run(feeds);

                IEnumerator<DisposableNamedOnnxValue> it = result.GetEnumerator();
                if (!it.MoveNext()) throw new InvalidOperationException("No outputs.");

                DenseTensor<float> t = (DenseTensor<float>)it.Current.Value;
                int rank = t.Dimensions.Length;

                float[] raw;
                if (rank == 2 && t.Dimensions[0] == 1 && t.Dimensions[1] == N)
                {
                    raw = t.ToArray(); // length N
                }
                else if (rank == 3 && t.Dimensions[0] == 1 && t.Dimensions[1] == N && t.Dimensions[2] == 1)
                {
                    // squeeze last dim
                    float[] arr = t.ToArray();
                    raw = new float[N];
                    for (i = 0; i < N; i++) raw[i] = arr[i];
                }
                else if (rank == 1 && t.Dimensions[0] == N)
                {
                    raw = t.ToArray();
                }
                else
                {
                    throw new InvalidOperationException("Unexpected detection output shape.");
                }

                // EXACTLY like your old PredictDetectionOriginal -> 0..100 rounded
                bool isSReversed = resourceName == "PAUTReader.Resources.Hybrid_DetLoc.onnx" ? true : false;
                return ProcessTensor(raw, isSReversed);
            }
            finally
            {
                if (result != null) result.Dispose();
            }
        }
        // ---- POSITION (cached, 2D wrapper, same semantics) ----
        private static InferenceSession _posSession;
        private static string _posKey;
        private static string _posInputName;

        public static void InitPositionSession(string resourceName)
        {
            if (_posSession != null && _posKey == resourceName) return;

            Stream s = null; MemoryStream ms = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                s = asm.GetManifestResourceStream(resourceName);

                if (_posSession != null) { _posSession.Dispose(); _posSession = null; }

                if (s != null)
                {
                    ms = new MemoryStream();
                    s.CopyTo(ms);
                    _posSession = new InferenceSession(ms.ToArray());
                }
                else
                {
                    _posSession = new InferenceSession(resourceName);
                }
                foreach (string k in _posSession.InputMetadata.Keys) { _posInputName = k; break; }
                _posKey = resourceName;
            }
            finally
            {
                if (ms != null) ms.Dispose();
                if (s != null) s.Dispose();
            }
        }

        // Returns (mins[ N ], maxs[ N ]) in 0..1 (you scale outside, like before)
        public static (float[], float[]) PredictPositionCached2D(string resourceName, float[,] data)
        {
            InitPositionSession(resourceName);

            int N = data.GetLength(0);
            int L = data.GetLength(1);

            DenseTensor<float> input = new DenseTensor<float>(new int[] { 1, N, L });
            int i, j;
            for (i = 0; i < N; i++)
                for (j = 0; j < L; j++)
                    input[0, i, j] = (float)data[i, j];

            List<NamedOnnxValue> feeds = new List<NamedOnnxValue>(1);
            feeds.Add(NamedOnnxValue.CreateFromTensor(_posInputName, input));

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> result = null;
            try
            {
                result = _posSession.Run(feeds);
                List<DisposableNamedOnnxValue> outs = new List<DisposableNamedOnnxValue>(result);

                float[] mins, maxs;

                if (outs.Count >= 3)
                {
                    DenseTensor<float> tMin = (DenseTensor<float>)outs[1].Value; // keep your historical indexing
                    DenseTensor<float> tMax = (DenseTensor<float>)outs[2].Value;
                    mins = To1D_N(tMin, N);
                    maxs = To1D_N(tMax, N);
                }
                else if (outs.Count == 2)
                {
                    DenseTensor<float> tMin = (DenseTensor<float>)outs[0].Value;
                    DenseTensor<float> tMax = (DenseTensor<float>)outs[1].Value;
                    mins = To1D_N(tMin, N);
                    maxs = To1D_N(tMax, N);
                }
                else if (outs.Count == 1)
                {
                    DenseTensor<float> t = (DenseTensor<float>)outs[0].Value; // expect [1,N,2] or [N,2]
                    if (t.Dimensions.Length == 3 && t.Dimensions[0] == 1 && t.Dimensions[1] == N && t.Dimensions[2] == 2)
                    {
                        float[] arr = t.ToArray();
                        mins = new float[N]; maxs = new float[N];
                        int k = 0;
                        for (i = 0; i < N; i++) { mins[i] = arr[k++]; maxs[i] = arr[k++]; }
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected position output shape.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("No outputs from position model.");
                }

                return (mins, maxs);
            }
            finally
            {
                if (result != null) result.Dispose();
            }
        }

        private static float[] To1D_N(DenseTensor<float> t, int N)
        {
            if (t.Dimensions.Length == 2 && t.Dimensions[0] == 1 && t.Dimensions[1] == N) return t.ToArray();
            if (t.Dimensions.Length == 3 && t.Dimensions[0] == 1 && t.Dimensions[1] == N && t.Dimensions[2] == 1)
            {
                float[] arr = t.ToArray();
                float[] y = new float[N];
                int i;
                for (i = 0; i < N; i++) y[i] = arr[i];
                return y;
            }
            if (t.Dimensions.Length == 1 && t.Dimensions[0] == N) return t.ToArray();
            throw new InvalidOperationException("Unsupported position output shape.");
        }

    }
}
