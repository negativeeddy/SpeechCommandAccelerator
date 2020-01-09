using Microsoft.CognitiveServices.Inventory.Speech;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Microsoft.CognitiveServices.Inventory
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
