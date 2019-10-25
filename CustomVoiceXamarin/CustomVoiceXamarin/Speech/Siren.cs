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

        private ISynthesizer synthesizer;

        public bool IsSirenStarted { get; private set; }
        public bool UseKeyWord { get; set; }

        private DialogServiceConnector botConnector = null;

        public Siren()
        {
            synthesizer = DependencyService.Get<ISynthesizer>();
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
                    await botConnector.StartKeywordRecognitionAsync(model);
                }
                else
                {
                    Trace.WriteLine("Starting inside else...");
                    await botConnector.ListenOnceAsync();
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


                DialogServiceConfig botConnectorConfig = DialogServiceConfig.FromBotSecret(BotSecret, SpeechSubscriptionKey, SpeechRegion);

                if (botConnectorConfig == null)
                {
                    Trace.WriteLine("BotConnectorConfig should not be null");
                }

                botConnectorConfig.SetProperty("DeviceGeometry", DeviceGeometry);
                botConnectorConfig.SetProperty("SelectedGeometry", SelectedGeometry);
                botConnectorConfig.SpeechRecognitionLanguage = LanguageRecognition;
                //botConnectorConfig.setProperty("SPEECH-LogFilename", logfile);


                AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput(); //run from the microphone

                botConnector = new DialogServiceConnector(botConnectorConfig, audioConfig);
                Trace.WriteLine("SpeechBotConnector created...");

                // Configure all event listeners
                RegisterEventListeners(botConnector);

                // Connect to the bot
                botConnector.ConnectAsync();
                Trace.WriteLine("SpeechBotConnector is successfully connected");

            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception thrown when connecting to SpeechBotConnector" + ex.ToString());
                botConnector.DisconnectAsync();             // disconnect bot.
            }
        }

        private void RegisterEventListeners(DialogServiceConnector botConnector)
        {
            // SessionStarted will notify when audio begins flowing to the service for a
            // turn
            botConnector.Recognizing += (o, e) => RecognizedText = e.Result.Text;

            // SessionStopped will notify when a turn is complete and it's safe to begin
            // listening again
            botConnector.Recognized += (o, e) =>
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
            botConnector.SessionStarted += (s, e) => Trace.WriteLine($"Session started event id: {e.SessionId}");

            // SessionStopped will notify when a turn is complete and it's safe to begin
            // listening again
            botConnector.SessionStopped += (s, e) => Trace.WriteLine($"Session stopped event id: {e.SessionId}");

            // Canceled will be signaled when a turn is aborted or experiences an error
            // condition
            botConnector.Canceled += (s, e) => Trace.WriteLine($"Cancelled event details: {e.ErrorDetails}");

            // ActivityReceived is the main way your bot will communicate with the client
            // and uses bot framework activities
            botConnector.ActivityReceived += (s, activityEventArgs) =>
            {
                string act = activityEventArgs.Activity;

                if (activityEventArgs.HasAudio)
                {
                    Trace.WriteLine("Audio Found");
                    synthesizer.PlayStream(activityEventArgs.Audio);
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
                RecognitionUpdate?.Invoke(this, new EventArgs());
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
                ResponseUpdated?.Invoke(this, new EventArgs());
            }
            get => _recognizedText;
        }

        public event EventHandler RecognitionUpdate;
        public event EventHandler ResponseUpdated;
        public event EventHandler RecognizedUpdate;

    }
}
