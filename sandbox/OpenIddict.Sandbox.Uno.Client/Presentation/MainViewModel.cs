namespace OpenIddict.Sandbox.UnoClient.Presentation;

using OpenIddict.Abstractions;
using OpenIddict.Client;

using Windows.UI.Popups;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigator navigator;
    private readonly OpenIddictClientService service;

    public bool IsProcessingIn => cancel is not null;
    public bool IsIdle => !IsProcessingIn;

    [ObservableProperty]
    private string? name;

    private CancellationTokenSource? cancel;
    private CancellationTokenSource? Cancel
    {
        get => cancel;
        set
        {
            cancel = value;
            OnPropertyChanged(nameof(IsProcessingIn));
            OnPropertyChanged(nameof(IsIdle));
            cancelCommand.NotifyCanExecuteChanged();
        }
    }

    private RelayCommand cancelCommand;
    public ICommand CancelCommand => cancelCommand;
    private void CancelLogout()
    {
        if (cancel is not null)
        {
            cancel.Cancel();
            cancel.Dispose();
            Cancel = null;
        }
    }

    private AsyncRelayCommand<string> logoutCommand;
    public ICommand LogoutCommand => logoutCommand;

    public string? Title { get; }

    public MainViewModel(
    IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        OpenIddictClientService service,
        INavigator navigator)
    {
        this.navigator = navigator;
        this.service = service;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
        logoutCommand = new AsyncRelayCommand<string>(provider => LogOutAsync(provider), _ => cancel is null);
        cancelCommand = new RelayCommand(CancelLogout, () => cancel is not null);
    }

    private async Task LogOutAsync(string provider, Dictionary<string, OpenIddictParameter>? parameters = null)
    {
        Cancel = new CancellationTokenSource();

        try
        {
            using var source = new CancellationTokenSource(delay: TimeSpan.FromSeconds(90));

            try
            {
                // Ask OpenIddict to initiate the logout flow (typically, by starting the system browser).
                var result = await service.SignOutInteractivelyAsync(new()
                {
                    AdditionalLogoutRequestParameters = parameters,
                    CancellationToken = source.Token,
                    ProviderName = provider
                });

                // Wait for the user to complete the logout process and authenticate the callback request.
                //
                // Note: in this case, only the claims contained in the state token can be resolved since
                // the authorization server doesn't return any other user identity during a logout dance.
                var authenticateTask = service.AuthenticateInteractivelyAsync(new()
                {
                    CancellationToken = source.Token,
                    Nonce = result.Nonce
                }).AsTask();
                if (await Task.WhenAny(authenticateTask, Task.Delay(TimeSpan.FromMinutes(5), cancel!.Token)) == authenticateTask)
                {
                    var dialog = new MessageDialog($"The user was successfully logged out from the {provider} server.",
                        "Logout demand successful");
                    await navigator.ShowMessageDialogAsync(dialog);
                    await navigator.NavigateViewModelAsync<LoginViewModel>(this);
                }
            }

            catch (OperationCanceledException)
            {
                var dialog = new MessageDialog("The logout process was aborted.",
                    "Logout timed out");
                await navigator.ShowMessageDialogAsync(dialog);
            }

            catch
            {
                var dialog = new MessageDialog("An error occurred while trying to log the user out.",
                    "Logout failed");
                await navigator.ShowMessageDialogAsync(dialog);
            }
        }

        finally
        {
            cancel?.Dispose();
            Cancel = null;
        }
    }
}
