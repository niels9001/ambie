using System;
using System.ComponentModel;
using AmbientSounds.Constants;
using AmbientSounds.Models;
using AmbientSounds.Services;
using AmbientSounds.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Windows.Services.Store;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace AmbientSounds.Views;

/// <summary>
/// The root frame used to power the backgrounds of the app.
/// </summary>
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();
        this.DataContext = App.Services.GetRequiredService<ShellPageViewModel>();
        ViewModel.MenuLabelsVisible = PageStateGroup.CurrentState == WidePageState;

        if (App.IsTenFoot)
        {
            // Ref: https://docs.microsoft.com/en-us/windows/uwp/xbox-apps/turn-off-overscan
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
        }

        ConnectToRoger();
    }

    public ShellPageViewModel ViewModel => (ShellPageViewModel)this.DataContext;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        var navigator = App.Services.GetRequiredService<INavigator>();

        if (navigator.Frame is null)
        {
            navigator.Frame = MainFrame;

            if (e.NavigationMode != NavigationMode.Back &&
                e.Parameter is ShellPageNavigationArgs args)
            {
                ViewModel.Navigate(args.FirstPageOverride ?? ContentPageType.Home);
            }
        }

        await ViewModel.InitializeAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        App.Services.GetRequiredService<INavigator>().Frame = null;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
    }

    private async void TeachingTip_ActionButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
    {
        ViewModel.IsRatingMessageVisible = false;
        var storeContext = StoreContext.GetDefault();
        var result = await storeContext.RequestRateAndReviewAppAsync();
        App.Services.GetRequiredService<IUserSettings>().Set(
            UserSettingsConstants.HasRated,
            true);
        App.Services.GetRequiredService<ITelemetry>().TrackEvent(
            TelemetryConstants.OobeRateUsClicked,
            new Dictionary<string, string>
            {
                { "status", result.Status.ToString() }
            });
    }

    private void TeachingTip_CloseButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
    {
        ViewModel.IsRatingMessageVisible = false;
        App.Services.GetRequiredService<IUserSettings>().SetAndSerialize(
            UserSettingsConstants.RatingDismissed,
            DateTime.UtcNow,
            AmbieJsonSerializerContext.Default.DateTime);
        App.Services.GetRequiredService<ITelemetry>().TrackEvent(TelemetryConstants.OobeRateUsDismissed);
    }

    private void OnMenuItemClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FrameworkElement f && f.FindParent<ListViewItem>() is { Tag: string tag })
        {
            switch (tag)
            {
                case "focus":
                    ViewModel.Navigate(ContentPageType.Focus);
                    break;
                case "catalogue":
                    ViewModel.Navigate(ContentPageType.Catalogue);
                    break;
                case "home":
                    ViewModel.Navigate(ContentPageType.Home);
                    break;
                case "settings":
                    ViewModel.Navigate(ContentPageType.Settings);
                    break;
                case "updates":
                    ViewModel.Navigate(ContentPageType.Updates);
                    break;
            }
        }
    }

    private void SizeStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        ViewModel.MenuLabelsVisible = PageStateGroup.CurrentState == WidePageState;
    }

    private async void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.FocusTimeBannerVisible))
        {
            if (ViewModel.FocusTimeBannerVisible)
            {
                await ShowTimeBannerAnimations.StartAsync();
            }
        }
    }


    private void ConnectToRoger()
    {
        RogerApi.Api.OnCommandRequestReceived += Api_OnCommandRequestReceived;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        RogerApi.Api.Connect(new RogerApi.AppDescription()
        {
            Name = "Ambie",
            Description = "Ambie is a sound editor text editor",
            Commands = new RogerApi.AppCommand[]
            {
                    new RogerApi.AppCommand()
                    {
                        Name = "getListOfSoundNames",
                        Description = "this will return a list of sound names"
                    }
}
        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    }

    private async void Api_OnCommandRequestReceived(object sender, RogerApi.CommandEventArgs e)
    {
        StringBuilder b = new StringBuilder();
        string ListOfSounds = b.ToString();
        //foreach (var i in ViewModel.Sounds)
        //{
        //    b.Append(i.Name);
        //}
        e.ReturnValue = Task<string>.Run(async () =>
        {
            string response = "";
            switch (e.CommandName)
            {
                case "getListOfSoundNames":
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        response = "dog.wav; cat.wav; dog.wav; cat.wav; dog.wav; cat.wav";
                    });
                    break;
            }
            return response;
        }).AsAsyncOperation<string>();

    }
}
