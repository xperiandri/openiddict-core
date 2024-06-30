namespace OpenIddict.Sandbox.UnoClient.Presentation;

using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

public sealed partial class LoginPage : Page
{
    // x:Bind does not support nested types
    private const string TwitterProviderName = Providers.Twitter;

    private LoginViewModel ViewModel => (LoginViewModel)DataContext;
    public LoginPage()
    {
        this.InitializeComponent();
    }
}
