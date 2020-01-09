using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Microsoft.CognitiveServices.Inventory.Speech
{
    /// <summary>
    /// The ISynthesier interface plays the audio streams from the Speech SDK
    /// It must be implemented in the platform specific libraries
    /// </summary>
    public interface ISynthesizer
    {
        /// <summary>
        /// Plays the audio stream.
        /// </summary>
        /// <param name="stream"></param>
        void PlayStream(PullAudioOutputStream stream);

        /// <summary>
        /// Initializes the device to receive data from the microphone and to 
        /// play audio
        /// </summary>
        /// <returns>a task</returns>
        Task InitializeAsync();
    }
}