using Microsoft.CognitiveServices.Inventory.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Xamarin.Forms;

[assembly: Dependency(typeof(Microsoft.CognitiveServices.Inventory.UWP.UwpSynthesizer))]
namespace Microsoft.CognitiveServices.Inventory.UWP
{
    public sealed class UwpSynthesizer : ISynthesizer
    {
        private AudioGraph _audioGraph;
        private AudioDeviceOutputNode _outputNode;
        private AudioFrameInputNode _frameInputNode;

        private readonly ConcurrentQueue<PullAudioOutputStream> _streamList = new ConcurrentQueue<PullAudioOutputStream>();

        private bool _isPlaying = false;

        public UwpSynthesizer()
        {
        }

        public async Task InitializeAsync()
        {
            await EnsureMicIsEnabled();
            await CreateAudioGraphAsync();
        }

        private static async Task EnsureMicIsEnabled()
        {
            bool isMicAvailable = true;
            try
            {
                var mediaCapture = new Windows.Media.Capture.MediaCapture();
                var settings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio;
                await mediaCapture.InitializeAsync(settings);
            }
            catch (Exception)
            {
                isMicAvailable = false;
            }

            if (!isMicAvailable)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-microphone"));
            }
            else
            {
                Trace.WriteLine("Microphone already enabled");
            }
        }

        /// <summary>
        /// Setup an AudioGraph with PCM input node and output for media playback
        /// </summary>
        private async Task CreateAudioGraphAsync()
        {
            AudioGraphSettings graphSettings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);
            CreateAudioGraphResult graphResult = await AudioGraph.CreateAsync(graphSettings);
            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                Trace.WriteLine($"Error in AudioGraph construction: {graphResult.Status.ToString()}");
            }

            _audioGraph = graphResult.Graph;

            CreateAudioDeviceOutputNodeResult outputResult = await _audioGraph.CreateDeviceOutputNodeAsync();
            if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Trace.WriteLine($"Error in audio OutputNode construction: {outputResult.Status.ToString()}");
            }

            _outputNode = outputResult.DeviceOutputNode;

            // Create the FrameInputNode using PCM format; 16kHz, 1 channel, 16 bits per sample
            AudioEncodingProperties nodeEncodingProperties = AudioEncodingProperties.CreatePcm(16000, 1, 16);
            _frameInputNode = _audioGraph.CreateFrameInputNode(nodeEncodingProperties);
            _frameInputNode.AddOutgoingConnection(_outputNode);

            // Initialize the FrameInputNode in the stopped state
            _frameInputNode.Stop();

            // Hook up an event handler so we can start generating samples when needed
            // This event is triggered when the node is required to provide data
            _frameInputNode.QuantumStarted += node_QuantumStarted;

            _audioGraph.Start();
        }

        private void node_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            uint numSamplesNeeded = (uint)args.RequiredSamples;

            if (numSamplesNeeded != 0)
            {
                AudioFrame audioData = ReadAudioData(numSamplesNeeded);
                _frameInputNode.AddFrame(audioData);
            }
        }

        private unsafe AudioFrame ReadAudioData(uint samples)
        {
            // Buffer size is (number of samples) * (size of each sample)
            uint bufferSize = samples * sizeof(byte) * 2;
            AudioFrame frame = new Windows.Media.AudioFrame(bufferSize);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                // Read audio data from the stream and copy it to the AudioFrame buffer
                var readBytes = new byte[capacityInBytes];
                
                PullAudioOutputStream _audioStream;
                uint bytesRead = 0;

                while (bytesRead == 0 && _streamList.TryPeek(out _audioStream))
                {
                    bytesRead = _audioStream.Read(readBytes);

                    if (bytesRead == 0)
                    {
                        _streamList.TryDequeue(out _audioStream);
                    }
                }

                if (bytesRead == 0 && _streamList.Count == 0)
                {
                    _frameInputNode.Stop();
                    _isPlaying = false;
                }

                for (int i = 0; i < bytesRead; i++)
                {
                    dataInBytes[i] = readBytes[i];
                }
            }

            return frame;
        }

        public void PlayStream(PullAudioOutputStream stream)
        {
            _streamList.Enqueue(stream);

            EnsureIsPlaying();
        }

        private object _startPlayingLock = new object();

        private void EnsureIsPlaying()
        {
            // prevent reentry
            lock (_startPlayingLock)
            {
                if (_isPlaying)
                {
                    return;
                }

                _frameInputNode.Start();
                _isPlaying = true;
            }
        }
    }
}
