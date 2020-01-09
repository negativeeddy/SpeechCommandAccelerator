using System;

namespace CustomVoiceXamarin.Speech
{
    public partial class SpeechCommandRecognizer
    {
        public class RecognitionEventArgs : EventArgs
        {
            public string Text { get; set; }
        }
    }
}
