using IdentityModel.OidcClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Uwp.Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const string AuthenticationScheme = "OpenIdConnect";
        private HttpClient client;


        public MainPage()
        {
            this.InitializeComponent();
        }

        private void GenralLoginButtonClick(object sender, RoutedEventArgs e)
        {
            Login();
        }

        public async void Login()
        {
            var options = new OidcClientOptions(
                authority: "https://localhost:44376",
                clientId: "UWP",
                clientSecret: "uwp_uwp_uwp",
                //scope: "openid profile email phone",
                scope: "openid profile email phone offline_access", // offline_access = refresh_token
                redirectUri: WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri,
                webView: new UwpWebView(enableWindowsAuthentication: false))
            {
                Style = OidcClientOptions.AuthenticationStyle.AuthorizationCode
            };

            var client = new OidcClient(options);
            var result = await client.LoginAsync();

            if (!string.IsNullOrEmpty(result.Error))
            {
                ResultTextBox.Text = result.Error;
                return;
            }

            var sb = new StringBuilder(128);

            foreach (var claim in result.Claims)
            {
                sb.AppendLine($"{claim.Type}: {claim.Value}");
            }

            sb.AppendLine($"refresh token: {result.RefreshToken}");
            sb.AppendLine($"access token: {result.AccessToken}");

            ResultTextBox.Text = sb.ToString();

            if (result.Handler == null)
            {
                this.client = new HttpClient();
            }
            else
            {
                this.client = new HttpClient(result.Handler);
            }
            this.client.SetBearerToken(result.AccessToken);
            this.client.BaseAddress = new Uri("https://localhost:44376/api/");

            await GetMessage();
        }

        private async Task GetMessage()
        {
            var response = await client.GetAsync("message");

            if (response.IsSuccessStatusCode)
                MessageTextBox.Text = await response.Content.ReadAsStringAsync();
            else
                MessageTextBox.Text = response.ReasonPhrase;
        }
    }
}
