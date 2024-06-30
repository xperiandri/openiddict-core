namespace OpenIddict.Sandbox.UnoClient.Presentation;

using CommunityToolkit.Mvvm.Input;

using OpenIddict.Abstractions;
using OpenIddict.Client;

using Windows.UI.Popups;

using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Abstractions.OpenIddictExceptions;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

public partial class LoginViewModel : ObservableObject
{
    public bool IsProcessingIn => cancel is not null;
    public bool IsIdle => !IsProcessingIn;

    private CancellationTokenSource? cancel;
    private CancellationTokenSource? Cancel
    {
        get => cancel;
        set
        {
            cancel = value;
            OnPropertyChanged(nameof(IsProcessingIn));
            OnPropertyChanged(nameof(IsIdle));
            loginCommand.NotifyCanExecuteChanged();
            loginWithParametersCommand.NotifyCanExecuteChanged();
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

    private AsyncRelayCommand<string> loginCommand;
    public ICommand LoginCommand => loginCommand;

    private AsyncRelayCommand<string> loginWithParametersCommand;
    public ICommand LoginWithParametersCommand => loginWithParametersCommand;

    private static Dictionary<string, OpenIddictParameter> githubLoginParameters =
        new Dictionary<string, OpenIddictParameter>()
        {
            [Parameters.IdentityProvider] = Providers.GitHub
        };
    private readonly OpenIddictClientService service;
    private readonly INavigator navigator;

    public LoginViewModel(OpenIddictClientService service, INavigator navigator)
    {
        this.service = service;
        this.navigator = navigator;
        loginCommand = new AsyncRelayCommand<string>(provider => LogInAsync(provider), _ => cancel is null);
        loginWithParametersCommand = new AsyncRelayCommand<string>(provider => LogInAsync(provider, githubLoginParameters), _ => cancel is null);
        cancelCommand = new RelayCommand(CancelLogout, () => cancel is not null);
    }

    private async Task LogInAsync(string? provider, Dictionary<string, OpenIddictParameter>? parameters = null)
    {
        Cancel = new CancellationTokenSource();

        try
        {
            using var source = new CancellationTokenSource(delay: TimeSpan.FromSeconds(90));

            try
            {
                // Ask OpenIddict to initiate the authentication flow (typically, by starting the system browser).
                var result = await service.ChallengeInteractivelyAsync(new()
                {
                    AdditionalAuthorizationRequestParameters = parameters,
                    CancellationToken = source.Token,
                    ProviderName = provider
                });

                var loginTask = service.AuthenticateInteractivelyAsync(new()
                {
                    CancellationToken = source.Token,
                    Nonce = result.Nonce
                }).AsTask();

                // Wait for the user to complete the authorization process and authenticate the callback request,
                // which allows resolving all the claims contained in the merged principal created by OpenIddict.
                if (await Task.WhenAny(loginTask, Task.Delay(TimeSpan.FromMinutes(5), cancel!.Token)) == loginTask)
                {
                    var principal = loginTask.Result.Principal;
                    await navigator.ShowMessageDialogAsync(this,
                            title: "Authentication successful",
                            content: $"Welcome, {principal.FindFirst(Claims.Name)!.Value}."
                        );
                    await navigator.NavigateViewModelAsync<MainViewModel>(this);
                }
            }

            catch (OperationCanceledException)
            {
                await navigator.ShowMessageDialogAsync(this,
                        title: "Authentication timed out",
                        content: "The authentication process was aborted."
                    );
            }

            catch (ProtocolException exception) when (exception.Error is Errors.AccessDenied)
            {
                await navigator.ShowMessageDialogAsync(this,
                        title: "Authorization denied",
                        content: "The authorization was denied by the end user."
                    );
            }

            catch
            {
                await navigator.ShowMessageDialogAsync(this,
                        title: "Authentication failed",
                        content: "An error occurred while trying to authenticate the user."
                    );
            }
        }

        finally
        {
            cancel?.Dispose();
            Cancel = null;
        }
    }
}
