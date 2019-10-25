using CustomVoiceXamarin.Speech;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CustomVoiceXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WWChataView : ContentPage
    {
        private Siren siren = null;

        public WWChataView()
        {
            InitializeComponent();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            messages.ItemsSource = _messages;

            await InitSiren();
        }

        private async Task InitSiren()
        {
            siren = new Siren();
            siren.RecognitionUpdate += Siren_RecognitionUpdate;
            siren.ResponseUpdated += Siren_ResponseUpdated;
            siren.RecognizedUpdate += Siren_RecognizedUpdate;

            siren.Initialize(); // initialize the speech channel bot connection
            siren.UseKeyWord = true;

            await siren.Start();
        }

        private void Siren_RecognizedUpdate(object sender, EventArgs e)
        {
            userSays(siren.RecognizedText);
        }

        private void Siren_ResponseUpdated(object sender, EventArgs e)
        {
            sirenSays(siren.ResultsText);
        }

        private void Siren_RecognitionUpdate(object sender, EventArgs e)
        {
            setText(siren.RecognizedText);
        }

        private void sirenSays(string resultsText)
        {
            var message = new Message
            {
                BelongsToCurrentUser = false,
                Text = resultsText,
                MemberData = new MemberData()
                {
                    Name = "Kira",
                    Color = "blue",
                },
            };

            _messages.Add(message);
        }

        private void setText(string recognizedText)
        {
            statusText.Text = recognizedText;
        }

        ObservableCollection<Message> _messages = new ObservableCollection<Message>();

        private void userSays(string recognizedText)
        {
            var message = new Message
            {
                BelongsToCurrentUser = true,
                Text = recognizedText,
                MemberData = new MemberData()
                {
                    Name = "User",
                    Color = "green",
                },
            };

            _messages.Add(message);
        }
    }
}