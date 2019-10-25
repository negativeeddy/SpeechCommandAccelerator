using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CustomVoiceXamarin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ModeSelectionPage : ContentPage
    {
        public ModeSelectionPage()
        {
            InitializeComponent();
        }
        private void bttnBadgeSelect_Clicked(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new BadgePage());
        }

        private void bttnWakeWordSelect_Clicked(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new WWChataView());
        }
    }
}