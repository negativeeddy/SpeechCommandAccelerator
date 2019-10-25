using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Dialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CustomVoiceXamarin.Speech
{
    class Siren
    {
        private string SpeechSubscriptionKey = "";
        private string SpeechRegion = "";
        private string Keyword = "";
        private string KeywordModel = "Hey_Kira.zip";
        private string DeviceGeometry = "";
        private string SelectedGeometry = "";
        private string LanguageRecognition = "";
        private string BotSecret = "";

        private string _recognizedText;
        private string szResultsText;
        private string logfile;

        private ISynthesizer _synthesizer;

        public bool IsSirenStarted { get; private set; }
        public bool UseKeyWord { get; set; }

        private DialogServiceConnector _dialogService = null;

        public Siren()
        {
            _synthesizer = DependencyService.Get<ISynthesizer>();
        }

        public async Task Start()
        {
            Trace.WriteLine("Start Called...");

            if (IsSirenStarted)
            {
                return;
            }

            IsSirenStarted = true;

            try
            {
                if (UseKeyWord)
                {
                    var model = KeywordRecognitionModel.FromFile(KeywordModel);   // TODO: will this work?
                    await _dialogService.StartKeywordRecognitionAsync(model);
                }
                else
                {
                    Trace.WriteLine("Starting inside else...");
                    await _dialogService.ListenOnceAsync();
                    // Start listening.
                    RecognizedText = "listen ...";
                }

            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception thrown during SpeechBotConnector start: " + e.ToString());
            }
        }

        public void Initialize()
        {
            try
            {
                RecognizedText = "Connecting to Bot";

                DialogServiceConfig dlgSvcConfig = DialogServiceConfig.FromBotSecret(BotSecret, SpeechSubscriptionKey, SpeechRegion);

                if (dlgSvcConfig == null)
                {
                    Trace.WriteLine("BotConnectorConfig should not be null");
                }

                dlgSvcConfig.SetProperty("DeviceGeometry", DeviceGeometry);
                dlgSvcConfig.SetProperty("SelectedGeometry", SelectedGeometry);
                dlgSvcConfig.SpeechRecognitionLanguage = LanguageRecognition;
                //botConnectorConfig.setProperty("SPEECH-LogFilename", logfile);


                AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput(); //run from the microphone

                _dialogService = new DialogServiceConnector(dlgSvcConfig, audioConfig);
                Trace.WriteLine("SpeechBotConnector created...");

                // Configure all event listeners
                RegisterEventListeners(_dialogService);

                // Connect to the bot
                _dialogService.ConnectAsync();
                Trace.WriteLine("SpeechBotConnector is successfully connected");

            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception thrown when connecting to SpeechBotConnector" + ex.ToString());
                _dialogService.DisconnectAsync();             // disconnect bot.
            }
        }

        private void RegisterEventListeners(DialogServiceConnector dlgSvcConnector)
        {
            // SessionStarted will notify when audio begins flowing to the service for a
            // turn
            dlgSvcConnector.Recognizing += (o, e) => RecognizedText = e.Result.Text;

            // SessionStopped will notify when a turn is complete and it's safe to begin
            // listening again
            dlgSvcConnector.Recognized += (o, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedKeyword)
                {
                    RecognizedText = "listen...";
                }
                else
                {
                    RecognizedText = e.Result.Text;
                }
            };

            // SessionStarted will notify when audio begins flowing to the service for a
            // turn
            dlgSvcConnector.SessionStarted += (s, e) => Trace.WriteLine($"Session started event id: {e.SessionId}");

            // SessionStopped will notify when a turn is complete and it's safe to begin
            // listening again
            dlgSvcConnector.SessionStopped += (s, e) => Trace.WriteLine($"Session stopped event id: {e.SessionId}");

            // Canceled will be signaled when a turn is aborted or experiences an error
            // condition
            dlgSvcConnector.Canceled += (s, e) => Trace.WriteLine($"Cancelled event details: {e.ErrorDetails}");

            // ActivityReceived is the main way your bot will communicate with the client
            // and uses bot framework activities
            dlgSvcConnector.ActivityReceived += (s, activityEventArgs) =>
            {
                string act = activityEventArgs.Activity;

                if (activityEventArgs.HasAudio)
                {
                    Trace.WriteLine("Audio Found");
                    _synthesizer.PlayStream(activityEventArgs.Audio);
                }

                try
                {
                    //JSONObject obj = new JSONObject(act);
                    //String sz = obj.getString("text");
                    //ResultsText = sz.Substring(6);
                    ResultsText = act; // TODO: extract the right info here base on the above comment
                }
                catch (Exception e)
                {
                    Trace.WriteLine("JSON handling issue " + e.Message);

                }

                IsSirenStarted = false;

                Trace.WriteLine("Received activity: {} " + act);
            };
        }

        public string RecognizedText
        {
            set
            {
                _recognizedText = " " + value;
                Trace.WriteLine(_recognizedText);
                RecognitionUpdate?.Invoke(this, new SirenEventArgs() { Text = value });
            }
            get => _recognizedText;
        }

        private string _resultsText;

        public string ResultsText
        {
            set
            {
                _resultsText = value;
                Trace.WriteLine(_resultsText);
                ResponseUpdated?.Invoke(this, new SirenEventArgs() { Text = value });
            }
            get => _recognizedText;
        }

        public event EventHandler<SirenEventArgs> RecognitionUpdate;
        public event EventHandler<SirenEventArgs> ResponseUpdated;
        public event EventHandler<SirenEventArgs> RecognizedUpdate;

        public class SirenEventArgs : EventArgs
        {
            public string Text { get; set; }
        }
    }
}
