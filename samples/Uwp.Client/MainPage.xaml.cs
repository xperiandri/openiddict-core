using JoseRT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Thinktecture.IdentityModel.Client;
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
        private static readonly OAuth2Client client = new OAuth2Client(new Uri("https://localhost:44376/connect/authorize"), "UWP", "uwp_uwp_uwp", OAuth2Client.ClientAuthenticationStyle.None);


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
            try
            {
                var nonce = DateTimeOffset.UtcNow.ToString();
                var logInUriString = client.CreateAuthorizeUrl(
                    clientId: "UWP",
                    responseType: "id_token",
                    scope: "openid profile email phone",
                    redirectUri: WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri,
                    nonce: nonce);

                var webAuthenticationResult = await WebAuthenticationBroker
                    .AuthenticateAsync(WebAuthenticationOptions.None, new Uri(logInUriString));

                await ProcessResponse(webAuthenticationResult);
            }
            catch
            {
                // TODO Handle
            }
        }

        private async Task ProcessResponse(WebAuthenticationResult webAuthenticationResult)
        {
            if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var response = new AuthorizeResponse(webAuthenticationResult.ResponseData);
                var token = JsonObject.Parse(Uri.UnescapeDataString(response.IdentityToken));
                var userInfoClient = new UserInfoClient(new Uri("https://localhost:44376/connect/userinfo"), response.IdentityToken);
                var userInfo = await userInfoClient.GetAsync();
                var user = userInfo.JsonObject;
                var claims = userInfo.Claims;
                int i = 5;
            }
            else if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
            {
                // do something when the request failed
            }
            else
            {
                // do something when an unknown error occurred
            }
        }

    }
}
