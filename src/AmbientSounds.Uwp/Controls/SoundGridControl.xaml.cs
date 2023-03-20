using AmbientSounds.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CopilotToolkit;
using AmbientSounds.Services;
using System;
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
        private void CopilotTextBox_MessageReceived(CopilotTextBox sender, string args)
        {
            var i = App.Services.GetRequiredService<IMixMediaPlayerService>();
            i.RemoveAll();
           
            System.Diagnostics.Debug.WriteLine(args);
            string[] selectedThemes = (args.ToLower().Replace("\n", "").Replace("\r", "")).Split(", ");

            foreach (var theme in selectedThemes)
            {
                string[] selectedTheme = theme.Split("-");
                string name = selectedTheme[0];
                double volume = Convert.ToDouble(selectedTheme[1]) * 0.1;
                System.Diagnostics.Debug.WriteLine("Tryihng: " + name);
                SoundViewModel m = ViewModel.Sounds.FirstOrDefault(x => x.Name!.ToLower() == name);
                if (m != null)
                {
                    System.Diagnostics.Debug.WriteLine(name + ": " + volume);
                    m.PlayCommand.Execute(null);
                    var y = i.GetVolume(m.Id);
               
                    i.SetVolume(m.Id, volume / 10);
                
                    
                    
                }
            }
        }
    }
}
