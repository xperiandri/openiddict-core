namespace OpenIddict.Sandbox.UnoClient.Presentation;

public sealed partial class LoginPage : Page
{
    private LoginViewModel ViewModel => (LoginViewModel)DataContext;
    public LoginPage()
    {
        this.InitializeComponent();
    }
}

