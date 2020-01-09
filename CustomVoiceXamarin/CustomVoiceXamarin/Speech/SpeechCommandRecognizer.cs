using Microsoft.Bot.Schema;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Dialog;
using Newtonsoft.Json;
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
    public partial class SpeechCommandRecognizer
    {
        private string SpeechRegion = "westus2";

        private string LanguageRecognition = "en-us";

        private string _recognizedText;

        private ISynthesizer _synthesizer;

        private bool _isListening = false;

        public bool IsListening
        {
            get => _isListening;
            private set
            {
                if (_isListening != value)
                {
                    _isListening = value;
                    ListeningChanged?.Invoke(this, new ListeningEventArgs { IsListening = _isListening });
                }
            }
        }

        public bool IsStarted { get; private set; }

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
                Trace.WriteLine("Starting listen once session.");
                await _dialogService.ListenOnceAsync();
                // Start listening.
                RecognizedText = "listening once ...";
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

                string speechApplicationId = AppSettings.Settings.GetValue("speechApplicationId");
                string speechSubscriptionKey = AppSettings.Settings.GetValue("speechSubscriptionKey");
                CustomCommandsConfig commandConfig = CustomCommandsConfig.FromSubscription(speechApplicationId, speechSubscriptionKey, SpeechRegion);

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
            // Recognizing will notify as the recognition happens displaying the 
            // currently recognized text
            dlgSvcConnector.Recognizing += (o, e) => RecognizedText = e.Result.Text;

            // Recognized will notify when a recognition is complete displaying the 
            // final recognized text
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
            dlgSvcConnector.SessionStarted += (s, e) =>
            {
                Trace.WriteLine($"SPEECH SESSION STARTED event id: {e.SessionId}");
                this.IsListening = true;
            };

            // SessionStopped will notify when a turn is complete and it's safe to begin
            // listening again
            dlgSvcConnector.SessionStopped += (s, e) =>
            {
                Trace.WriteLine($"SPEECH SESSION STOPPED event id: {e.SessionId}");
                this.IsStarted = false;
                this.IsListening = false;
            };

            // Canceled will be signaled when a turn is aborted or experiences an error
            // condition
            dlgSvcConnector.Canceled += (s, e) =>
            {
                Trace.WriteLine($"SPEECH CANCELLED event details: {e.ErrorDetails}");
                RecognizedText = e.ErrorDetails;
                this.IsStarted = false;
                this.IsListening = false;
            };

            // ActivityReceived is the main way your bot will communicate with the client
            // and uses bot framework activities
            dlgSvcConnector.ActivityReceived += async (s, activityEventArgs) =>
            {
                string activityJson = activityEventArgs.Activity;

                if (activityEventArgs.HasAudio)
                {
                    Trace.WriteLine("Audio Found");
                    _synthesizer.PlayStream(activityEventArgs.Audio);
                }

                try
                {
                    var activity = JsonConvert.DeserializeObject<Activity>(activityJson);

                    // use the spoken text if available, otherwise the normal text
                    ResultsText = activity.Speak ?? activity.Text;

                    if (activity.InputHint == InputHints.ExpectingInput)
                    {
                        // restart the listening since the command has indicated it is
                        // waiting for a reply to this activity
                        await _dialogService.ListenOnceAsync();
                        IsStarted = true;
                    }
                    else
                    {
                        IsStarted = false;
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine("JSON handling issue " + e.Message);

                }

                Trace.WriteLine($"Received activity: {activityJson}");
            };
        }

        public string RecognizedText
        {
            set
            {
                _recognizedText = " " + value;
                Trace.WriteLine(_recognizedText);
                RecognitionUpdate?.Invoke(this, new RecognitionEventArgs() { Text = value });
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

        public event EventHandler<RecognitionEventArgs> RecognitionUpdate;
        public event EventHandler<RecognitionEventArgs> ResponseUpdated;
        public event EventHandler<RecognitionEventArgs> RecognizedUpdate;
        public event EventHandler<ListeningEventArgs> ListeningChanged;
    }
}
