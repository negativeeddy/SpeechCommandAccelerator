using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Dialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace CustomVoiceXamarin.Speech
{
    public class SpeechCommandRecognizer
    {
        private string SpeechApplicationId = "5157984f-b198-4d96-b2da-31d08edba1ee";
        private string SpeechSubscriptionKey = "b5a192fa686c46ba9ba16d5b1553769f";
        private string SpeechRegion = "westus2";
        private string KeywordModel = @"C:\src\CustomVoiceXamarin\CustomVoiceXamarin\CustomVoiceXamarin.UWP\bin\x86\Debug\AppX\voice\Hey_Kira.zip";

        private string LanguageRecognition = "en-us";

        private string _recognizedText;

        private ISynthesizer _synthesizer;

        public bool IsStarted { get; private set; }
        public bool UseKeyWord { get; set; }

        private DialogServiceConnector _dialogService = null;

        public SpeechCommandRecognizer(ISynthesizer synthesizer)
        {
            _synthesizer = synthesizer;
        }

        public async Task StartAsync()
        {
            Trace.WriteLine("Starting Siren...");

            if (IsStarted)
            {
                return;
            }

            IsStarted = true;

            try
            {
                if (UseKeyWord)
                {
                    var model = KeywordRecognitionModel.FromFile(KeywordModel);   // TODO: will this work?
                    await _dialogService.StartKeywordRecognitionAsync(model);
                }
                else
                {
                    Trace.WriteLine("Starting listen once session.");
                    await _dialogService.ListenOnceAsync();
                    // Start listening.
                    RecognizedText = "listening once ...";
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception thrown during SpeechBotConnector start: " + e.ToString());
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _synthesizer.InitializeAsync();

                RecognizedText = "Connecting to assistant";

                CustomCommandsConfig commandConfig = CustomCommandsConfig.FromSubscription(SpeechApplicationId, SpeechSubscriptionKey, SpeechRegion);

                if (commandConfig == null)
                {
                    Trace.WriteLine("BotConnectorConfig should not be null");
                }

                commandConfig.Language = LanguageRecognition;

                AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput(); //run from the microphone

                _dialogService = new DialogServiceConnector(commandConfig, audioConfig);

                // Configure all event listeners
                RegisterEventListeners(_dialogService);

                // Connect to the bot
                await _dialogService.ConnectAsync();
                Trace.WriteLine("SpeechBotConnector is successfully connected");

            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception thrown when connecting to SpeechBotConnector" + ex.ToString());
                await _dialogService.DisconnectAsync();             // disconnect bot.
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
                    RecognizedText = "listening...";
                }
                else
                {
                    RecognizedText = e.Result.Text;
                }
            };

            // SessionStarted will notify when audio begins flowing to the service for a
            // turn
            dlgSvcConnector.SessionStarted += (s, e) => Trace.WriteLine($"SPEECH SESSION STARTED event id: {e.SessionId}");

            // SessionStopped will notify when a turn is complete and it's safe to begin
            // listening again
            dlgSvcConnector.SessionStopped += (s, e) =>
            {
                Trace.WriteLine($"SPEECH SESSION STOPPED event id: {e.SessionId}");
                this.IsStarted = false;
            };

            // Canceled will be signaled when a turn is aborted or experiences an error
            // condition
            dlgSvcConnector.Canceled += (s, e) =>
            {
                Trace.WriteLine($"SPEECH CANCELLED event details: {e.ErrorDetails}");
                this.IsStarted = false;
            };

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

                IsStarted = false;

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
                //ResponseUpdated?.Invoke(this, new SirenEventArgs() { Text = value });
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
