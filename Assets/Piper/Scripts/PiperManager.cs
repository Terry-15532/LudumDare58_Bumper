using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.InferenceEngine;
using UnityEngine;

namespace Piper
{
    public class PiperManager : MonoBehaviour
    {
        public Unity.InferenceEngine.BackendType backend = Unity.InferenceEngine.BackendType.GPUCompute;
        public Unity.InferenceEngine.ModelAsset model;

        public string voice = "en-us";
        public int sampleRate = 22050;

        private Unity.InferenceEngine.Model _runtimeModel;
        private Worker _worker;

        private static readonly float[] Scales = { 0.667f, 1f, 0.8f };
        private Tensor<float> _scalesTensor;

        private void Awake()
        {
            var espeakPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");
            PiperWrapper.InitPiper(espeakPath);

            _runtimeModel = Unity.InferenceEngine.ModelLoader.Load(model);
            _worker = new Worker(_runtimeModel, backend);

            _scalesTensor = new Tensor<float>(new TensorShape(3), Scales);
        }

        public async Task<AudioClip> TextToSpeechAsync(string text)
        {
            // 1) 纯 CPU 预处理：放到后台线程
            var phonemes = await Task.Run(() => PiperWrapper.ProcessText(text, voice));

            // 2) 预估容量，减少 List 扩容
            var audioBuffer = new List<float>(8192);

            // 3) 推理和回读：保留在 Unity/主线程上下文
            for (int i = 0; i < phonemes.Sentences.Length; i++)
            {
                var sentence = phonemes.Sentences[i];
                var inputPhonemes = sentence.PhonemesIds;

                using var inputTensor = new Tensor<int>(
                    new TensorShape(1, inputPhonemes.Length),
                    inputPhonemes
                );

                using var inputLengthsTensor = new Tensor<int>(
                    new TensorShape(1),
                    new int[] { inputPhonemes.Length }
                );

                _worker.Schedule(inputTensor, inputLengthsTensor, _scalesTensor);

                var outputTensor = _worker.PeekOutput() as Tensor<float>;
                if (outputTensor == null)
                    continue;

                using var cpuCopyTensor = await outputTensor.ReadbackAndCloneAsync();
                var output = cpuCopyTensor.DownloadToArray();

                if (output != null && output.Length > 0)
                    audioBuffer.AddRange(output);
            }

            // 4) 音频对象创建/写入保持在主线程
            var audioClip = AudioClip.Create("piper_tts", audioBuffer.Count, 1, sampleRate, false);
            audioClip.SetData(audioBuffer.ToArray(), 0);
            return audioClip;
        }

        private void OnDestroy()
        {
            _scalesTensor?.Dispose();
            _worker?.Dispose();
            PiperWrapper.FreePiper();
        }
    }
}