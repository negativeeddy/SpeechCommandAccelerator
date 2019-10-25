using CustomVoiceXamarin.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(CustomVoiceXamarin.UWP.Synthesizer))]
namespace CustomVoiceXamarin.UWP
{
    public class Synthesizer : ISynthesizer
    {
        public void PlayStream(PullAudioOutputStream stream)
        {
        }
    }
}
