using Microsoft.CognitiveServices.Speech.Conversation;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace CustomVoiceXamarin
{

    public class ChatTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BotTemplate { get; set; }
        public DataTemplate UserTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return ((Message)item).BelongsToCurrentUser ? UserTemplate : BotTemplate;
        }
    }
}
