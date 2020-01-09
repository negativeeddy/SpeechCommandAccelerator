using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Media;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using CustomVoiceXamarin.Speech;
using Java.IO;
using Microsoft.CognitiveServices.Speech.Audio;
using MediaEncoding = Android.Media.Encoding;

[assembly: Xamarin.Forms.Dependency(typeof(CustomVoiceXamarin.Droid.AndroidSynthesizer))]

namespace CustomVoiceXamarin.Droid
{
    public sealed class AndroidSynthesizer : ISynthesizer
    {
        private const string LOG_TAG = "Synthesizer";
        private const int SAMPLE_RATE = 16000;
        private const MediaEncoding ENCODING = MediaEncoding.Pcm16bit;
        private const ChannelOut CHANNEL_CONFIG = ChannelOut.Mono;

        private readonly ConcurrentQueue<PullAudioOutputStream> _streamList = new ConcurrentQueue<PullAudioOutputStream>();

        private int _playBufSize;
        private string _audioFileDirectory = "audioFiles";
        private string _audioFileName = "audioResponse";
        private string _audioFileExtension = "wav";
        private bool _isPlaying = false;

        public AndroidSynthesizer()
        {
        }

        public void PlayStream(PullAudioOutputStream stream)
        {
            _streamList.Enqueue(stream);

            EnsureIsPlaying();
        }

        private object _startPlayingLock = new object();
        private bool _micAccessGranted;

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

            _playBufSize = AudioTrack.GetMinBufferSize(SAMPLE_RATE, CHANNEL_CONFIG, ENCODING);

            AudioAttributes attrs = new AudioAttributes.Builder()
                    .SetContentType(AudioContentType.Speech)
                    .SetUsage(AudioUsageKind.Media)
                    .Build();

            AudioFormat fmt = new AudioFormat.Builder()
                    .SetChannelMask(CHANNEL_CONFIG)
                    .SetEncoding(ENCODING)
                    .SetSampleRate(SAMPLE_RATE)
                    .Build();

            var audioTrack = new AudioTrack(attrs, fmt, _playBufSize, AudioTrackMode.Stream, 0);


            Task.Run(async () =>
            {

                using (FileOutputStream fileStream = getFileOutputStream())
                {
                    byte[] buffer = new byte[_playBufSize];
                    audioTrack.Play();
                    long readSize = -1;

                    while (_streamList.TryDequeue(out PullAudioOutputStream stream))
                    {
                        try
                        {
                            while (readSize != 0)
                            {
                                readSize = stream.Read(buffer);
                                await audioTrack.WriteAsync(buffer, 0, (int)readSize);
                                if (fileStream != null)
                                {
                                    await fileStream.WriteAsync(buffer);
                                }
                            }

                            //readSize = in.read(buffer);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.WriteLine(e.ToString(), LOG_TAG);
                            break;
                        }
                        //audioTrack.write(buffer, 0, readSize);
                    }

                    audioTrack.Stop();

                    _isPlaying = false;

                    try
                    {
                        if (fileStream != null)
                        {
                            fileStream.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.WriteLine(e, LOG_TAG);
                    }
                    // trigger event that playback is stopped
                    //EventBus.getDefault().post(new SynthesizerStopped());
                }
            });
        }

        private FileOutputStream getFileOutputStream()
        {
            string fileName = string.Format("%s/%s.%d.%s",
                    _audioFileDirectory,
                    _audioFileName,
                    DateTime.Now.Ticks,
                    _audioFileExtension);
            try
            {
                File file = new File(fileName);

                if (file.ParentFile != null)
                    file.Mkdir();

                if (!file.Exists())
                {
                    file.CreateNewFile();
                }
                return new FileOutputStream(fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex, LOG_TAG);
            }
            return null;
        }

        public async Task InitializeAsync()
        {
            await EnsureMicIsEnabled();
        }

        private async Task EnsureMicIsEnabled()
        {
            // Permissions are required only for Marshmallow and up
            if ((int)Build.VERSION.SdkInt < 23)
            {
                return;
            }
            else
            {
                var currentActivity = MainActivity.Instance;
                if ((ActivityCompat.CheckSelfPermission(currentActivity, Manifest.Permission.RecordAudio) != (int)Android.Content.PM.Permission.Granted) ||
                    ActivityCompat.CheckSelfPermission(currentActivity, Manifest.Permission.ReadExternalStorage) != (int)Android.Content.PM.Permission.Granted)
                {
                    RequestMicPermission();
                }
            }
        }


        public const int REQUEST_MIC = 1;
        private string[] permissions = { Manifest.Permission.RecordAudio, Manifest.Permission.ReadExternalStorage };

        private void RequestMicPermission()
        {
            var currentActivity = MainActivity.Instance;
            if (ActivityCompat.ShouldShowRequestPermissionRationale(currentActivity, Manifest.Permission.RecordAudio))
            {
                Snackbar.Make(currentActivity.FindViewById((Android.Resource.Id.Content)), "App requires microphone permission.", Snackbar.LengthIndefinite).SetAction("Ok", v =>
                {
                    ((Activity)currentActivity).RequestPermissions(permissions, REQUEST_MIC);

                }).Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(((Activity)currentActivity), permissions, REQUEST_MIC);
            }
        }

    }
}