namespace OpenIddict.Sandbox.UnoClient.Presentation;

public sealed partial class MainPage : Page
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    public MainPage()
    {
        this.InitializeComponent();
    }
}
