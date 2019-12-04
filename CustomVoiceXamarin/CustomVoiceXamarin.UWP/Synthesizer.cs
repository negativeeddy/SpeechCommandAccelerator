using CustomVoiceXamarin.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Xamarin.Forms;

[assembly: Dependency(typeof(CustomVoiceXamarin.UWP.Synthesizer))]
namespace CustomVoiceXamarin.UWP
{
    public sealed class Synthesizer : ISynthesizer
    {
        private AudioGraph _audioGraph;
        private AudioDeviceOutputNode _outputNode;
        private AudioFrameInputNode _frameInputNode;
        private PullAudioOutputStream _audioStream;

        private const string LOG_TAG = "Synthesizer";
        private const int SAMPLE_RATE = 16000;
        private const string AudioFileDirectory = "audioFiles";
        private const string AudioFileName = "audioResponse";
        private const string AudioFileExtension = "wav";
        private const string ContentType = "audio/x-wav";

        //private const MediaEncoding ENCODING = MediaEncoding.Pcm16bit;
        //private const ChannelOut CHANNEL_CONFIG = ChannelOut.Mono;

        private readonly ConcurrentQueue<PullAudioOutputStream> _streamList = new ConcurrentQueue<PullAudioOutputStream>();

        private bool _isPlaying = false;

        public Synthesizer()
        {
            _player.MediaFailed += (s,e) => Trace.WriteLine($"MediaPlayer failed: {e.Error} : {e.ErrorMessage}"); 
            _player.CurrentStateChanged += (s,_) => Trace.WriteLine($"MediaPlayer state changed: {_player.PlaybackSession.PlaybackState}"); 
            _player.MediaOpened += (s,_) => Trace.WriteLine($"MediaPlayer media opened: {_player.PlaybackSession.PlaybackState}");
            _player.MediaEnded += (s,_) => Trace.WriteLine($"MediaPlayer media ended: {_player.PlaybackSession.PlaybackState}");
            _player.AudioCategory = MediaPlayerAudioCategory.Speech;

            var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
        }

        private void _player_CurrentStateChanged(MediaPlayer sender, object args)
        {
            throw new NotImplementedException();
        }

        private void _player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Trace.WriteLine($"MediaFailed: {args.Error} : {args.ErrorMessage}");
        }

        public async Task InitializeAsync()
        {
            await EnsureMicIsEnabled();
            await CreateAudioGraph();
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
        private async Task CreateAudioGraph()
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
                uint bytesRead = _audioStream.Read(readBytes);

                if (bytesRead == 0)
                {
                    _frameInputNode.Stop();
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
                MediaPlayer _player = new MediaPlayer();

        // TODO: switch to BlockingCollection<T> for producer/consumer pattern
        private void EnsureIsPlaying()
        {
            // prevent reentry
            lock (_startPlayingLock)
            {
                if (_isPlaying)
                {
                    return;
                }

                _isPlaying = true;
            }

            Task.Run(async () =>
            {
                const int _playBufSize = 4096;

                {
                    byte[] buffer = new byte[_playBufSize];
                    //audioTrack.Play();

                    while (_streamList.TryDequeue(out PullAudioOutputStream stream))
                    {
                        _audioStream = stream;
                        _frameInputNode.Start();

                        //try
                        //{
                        //    // read the SDK stream into memory
                        //    MemoryStream memStream = new MemoryStream();
                        //    long readSize = -1;
                        //    while (readSize != 0)
                        //    {
                        //        readSize = stream.Read(buffer);
                        //        await memStream.WriteAsync(buffer, 0, (int)readSize);
                        //    }

                        //    memStream.Seek(0, SeekOrigin.Begin);
                        //    await SaveToFile(memStream);
                        //    memStream.Seek(0, SeekOrigin.Begin);

                        //    // play the stream
                        //    var mediaSource = MediaSource.CreateFromStream(memStream.AsRandomAccessStream(), ContentType);
                        //    _player.Source = mediaSource;
                        //    _player.Play();

                        //}
                        //catch (Exception e)
                        //{
                        //    System.Diagnostics.Trace.WriteLine(e.ToString(), LOG_TAG);
                        //    break;
                        //}
                        //audioTrack.write(buffer, 0, readSize);
                    }

                    //audioTrack.Stop();

                    _isPlaying = false;

                    // trigger event that playback is stopped
                    //EventBus.getDefault().post(new SynthesizerStopped());
                }
            });
        }

        private async Task SaveToFile(MemoryStream memStream)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile file = await localFolder.CreateFileAsync($"{AudioFileName}.{DateTime.Now.Ticks}.{AudioFileExtension}", CreationCollisionOption.OpenIfExists);
                Trace.WriteLine($"Created file {file.Path}");
                using (var fileStream = await file.OpenStreamForWriteAsync())
                {
                        await memStream.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                        fileStream.Close();
                }
            }
            catch (Exception e)
            {
                // dont let failure to save to disk cause any other issues
                System.Diagnostics.Trace.WriteLine(e, LOG_TAG);
            }

        }
    }
}
