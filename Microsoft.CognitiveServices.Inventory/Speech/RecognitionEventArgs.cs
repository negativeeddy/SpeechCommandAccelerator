using System;

namespace Microsoft.CognitiveServices.Inventory.Speech
{
    public partial class SpeechCommandRecognizer
    {
        public class RecognitionEventArgs : EventArgs
        {
            public string Text { get; set; }
        }
    }
}
