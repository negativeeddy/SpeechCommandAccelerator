using Microsoft.CognitiveServices.Inventory.Speech;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Microsoft.CognitiveServices.Inventory
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BadgePage : ContentPage
    {
        private SpeechCommandRecognizer _siren = null;
        private ObservableCollection<Message> _messages = new ObservableCollection<Message>();

        public BadgePage()
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
            _siren = new SpeechCommandRecognizer(DependencyService.Get<ISynthesizer>());
            _siren.RecognitionUpdate += (s, e) => setText(e.Text);
            _siren.ResponseUpdated += (s, e) => AddBotText(e.Text);
            _siren.RecognizedUpdate += (s, e) => AddUserText(e.Text);
            _siren.ListeningChanged += (s, e) => UpdateListening(e.IsListening);

            await _siren.InitializeAsync(); // initialize the speech channel bot connection
        }

        private void UpdateListening(bool isListening)
        {
            UpdateUI(() => cbIsListening.IsChecked = isListening);
        }

        private void UpdateUI(Action action)
        {
            if (MainThread.IsMainThread)
            {
                action();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(action);
            }
        }

        private void setText(string recognizedText)
        {
            UpdateUI(() => statusText.Text = recognizedText);
        }

        private void AddBotText(string text)
        {
            UpdateUI(() =>
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
            });
        }

        private void AddUserText(string text)
        {
            UpdateUI(() =>
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
            });
        }

        private async void bttnStartListening_Clicked(object sender, EventArgs e)
        {
            await _siren.StartAsync();
        }
    }
}