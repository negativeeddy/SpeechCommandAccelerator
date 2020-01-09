using System;

namespace CustomVoiceXamarin.Speech
{
    public partial class SpeechCommandRecognizer
    {
        public class ListeningEventArgs : EventArgs
        {
            public bool IsListening { get; set; }
        }
    }
}
