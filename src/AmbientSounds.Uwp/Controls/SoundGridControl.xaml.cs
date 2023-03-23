using AmbientSounds.Services;
using AmbientSounds.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#nullable enable

namespace AmbientSounds.Controls
{
    public sealed partial class SoundGridControl : UserControl
    {
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(SoundGridControl),
            null);

        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(
                nameof(IsCompact),
                typeof(bool),
                typeof(SoundGridControl),
                new PropertyMetadata(false));

        public SoundGridControl()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SoundListViewModel>();
            this.Loaded += async (_, _) => { await ViewModel.LoadCommand.ExecuteAsync(null); };
            this.Unloaded += (_, _) => { ViewModel.Dispose(); };
            ConnectToRoger();

            System.Diagnostics.Debug.WriteLine(GetSoundtracksNames());
            PlaySoundtracks("beach;underwater;rain");
        }

        public SoundListViewModel ViewModel => (SoundListViewModel)this.DataContext;

        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate?)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public bool IsCompact
        {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        public void ConnectToRoger()
        {
            RogerApi.Api.OnCommandRequestReceived += this.Api_OnCommandRequestReceived;
            RogerApi.Api.Connect(new RogerApi.AppDescription()
            {
                Name = "Ambie",
                Description = "Ambie is a ambient sound player app.",
                StarterSuggestions = new string[] { "Get all soundtracks from this app" },
                Commands = new RogerApi.AppCommand[]
                 {
                    new RogerApi.AppCommand()
                    {
                        Name = "getSoundtracks",
                        Description = "gets a semi-colon seperated list of all available soundtracks",
                    },
                    new RogerApi.AppCommand()
                    {
                        Name = "playSoundtracks",
                        Description = "posts a semi-colon seperated list of soundtracks that need to be played",
                        Parameters = new string[] { "list" }
                    },
                 },
            });
        }

        private void Api_OnCommandRequestReceived(object sender, RogerApi.CommandEventArgs e)
        {
            e.ReturnValue = Task<string>.Run(async () =>
            {
                string response = "";
                switch (e.CommandName)
                {
                    case "getSoundtracks":
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            response = GetSoundtracksNames();
                        });
                        break;
                    case "playSoundtracks":
                        if (e.Params["list"] != null)
                        {
                            PlaySoundtracks(e.Params["list"]);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ROGER ERROR: error getting text to insert: " + e.Params.ToString());
                        }
                        response = "done";
                        break;
                }
                return response;
            }).AsAsyncOperation<string>();
        }

        public void PlaySoundtracks(string command)
        {
            var i = App.Services.GetRequiredService<IMixMediaPlayerService>();
            i.RemoveAll();

            string[] selectedThemes = (command.ToLower().Replace("\n", "").Replace("\r", "")).Split(";");

            foreach (SoundViewModel s in ViewModel.Sounds)
            {
                if (selectedThemes.Contains(s.Name))
                {
                    s.PlayCommand.Execute(null);
                }
            }
        }

        private string GetSoundtracksNames()
        {
            StringBuilder b = new StringBuilder();
            foreach (SoundViewModel s in ViewModel.Sounds)
            { 
                b.Append(s.Name);
                b.Append(";");
            }
            return b.ToString();
        }
    }
}
