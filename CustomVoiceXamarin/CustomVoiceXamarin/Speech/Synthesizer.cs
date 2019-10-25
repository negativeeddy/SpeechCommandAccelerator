using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CognitiveServices.Speech.Audio;

namespace CustomVoiceXamarin.Speech
{
    public interface ISynthesizer
    {
        void PlayStream(PullAudioOutputStream stream);
    }

}