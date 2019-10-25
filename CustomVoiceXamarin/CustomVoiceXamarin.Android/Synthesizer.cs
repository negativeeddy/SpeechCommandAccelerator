using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CustomVoiceXamarin.Speech;
using Java.IO;
using Java.Util.Concurrent.Atomic;
using Microsoft.CognitiveServices.Speech.Audio;
using MediaEncoding = Android.Media.Encoding;

[assembly: Xamarin.Forms.Dependency(typeof(CustomVoiceXamarin.Droid.Synthesizer))]

namespace CustomVoiceXamarin.Droid
{
    public class Synthesizer : ISynthesizer
    {
        private const string logTag = "Synthesizer";
        private const int SAMPLE_RATE = 16000;
        private const MediaEncoding ENCODING = MediaEncoding.Pcm16bit;
        private const ChannelOut CHANNEL_CONFIG = ChannelOut.Mono;

        private readonly ConcurrentQueue<PullAudioOutputStream> streamList;
        
        private int _playBufSize;
        private string _audioFileDirectory = "audioFiles";
        private string _audioFileName = "audioResponse";
        private string _audioFileExtension = "wav";
        private bool _isPlaying = false;

        public Synthesizer()
        {
            this.streamList = new ConcurrentQueue<PullAudioOutputStream>();
        }

        public void PlayStream(PullAudioOutputStream stream)
        {
            streamList.Enqueue(stream);

            if (!_isPlaying)
            {
                startPlaying();
            }
        }

        private object startPlayingLock = new object();

        // TODO: switch to BlockingCollection<T> for producer/consumer pattern
        private void startPlaying()
        {
            // prevent reentry
            lock (startPlayingLock)
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

                using (FileOutputStream fos = getFileOutputStream())
                {
                    byte[] buffer = new byte[_playBufSize];
                    audioTrack.Play();
                    long readSize = -1;

                    while (streamList.TryDequeue(out PullAudioOutputStream stream))
                    {
                        try
                        {
                            while (readSize != 0)
                            {
                                readSize = stream.Read(buffer);
                                await audioTrack.WriteAsync(buffer, 0, (int)readSize);
                                if (fos != null)
                                {
                                    await fos.WriteAsync(buffer);
                                }
                            }

                            //readSize = in.read(buffer);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.WriteLine(e.ToString(), logTag);
                            break;
                        }
                        //audioTrack.write(buffer, 0, readSize);
                    }

                    audioTrack.Stop();

                    _isPlaying = false;

                    try
                    {
                        if (fos != null)
                        {
                            fos.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.WriteLine(e);
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
                System.Diagnostics.Trace.WriteLine(ex);
            }
            return null;
        }
    }
}