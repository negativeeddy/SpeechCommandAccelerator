using System;

namespace Microsoft.CognitiveServices.Inventory.Speech
{
    public partial class SpeechCommandRecognizer
    {
        public class ListeningEventArgs : EventArgs
        {
            public bool IsListening { get; set; }
        }
    }
}
