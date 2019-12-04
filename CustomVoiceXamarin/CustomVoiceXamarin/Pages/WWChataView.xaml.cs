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
using static CustomVoiceXamarin.Speech.Siren;

namespace CustomVoiceXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WWChataView : ContentPage
    {
        private Siren _siren = null;
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();
        //{
        //    new Message {
        //        Text = "test",
        //        BelongsToCurrentUser = true,
        //        MemberData = new MemberData
        //        {
        //            Name="Bob",
        //            Color  = Color.Red,
        //        },
        //    },
        //    new Message {
        //        Text = "test",
        //        BelongsToCurrentUser = false,
        //        MemberData = new MemberData
        //        {
        //            Name="Sue",
        //            Color  = Color.Green,
        //        },
        //    },
        //};

        public WWChataView()
        {
            InitializeComponent();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            messagesView.ItemsSource = _messages;

            await InitSiren();
        }

        private async Task InitSiren()
        {
            _siren = new Siren();
            _siren.RecognitionUpdate += (s, e) => setText(e.Text);
            _siren.ResponseUpdated += (s, e) => AddBotText(e.Text);
            _siren.RecognizedUpdate += (s, e) => AddUserText(e.Text);


            await _siren.InitializeAsync(); // initialize the speech channel bot connection
            _siren.UseKeyWord = true;

            await _siren.StartAsync();
        }

        private void setText(string recognizedText)
        {
            statusText.Text = recognizedText;
        }

        private void AddBotText(string text)
        {
            var message = new Message
            {
                BelongsToCurrentUser = false,
                Text = text,
                MemberData = new MemberData()
                {
                    Name = "Kira",
                    Color = Color.Blue,
                },
            };

            _messages.Add(message);
        }

        private void AddUserText(string text)
        {
            var message = new Message
            {
                BelongsToCurrentUser = true,
                Text = text,
                MemberData = new MemberData()
                {
                    Name = "User",
                    Color = Color.Green,
                },
            };

            _messages.Add(message);
        }
    }
}