using System.Net.Http;

using Microsoft.EntityFrameworkCore;

using OpenIddict.Client;

using Uno.Resizetizer;

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.Sandbox.UnoClient;
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = await this.CreateBuilder(args)
            .UseOpenIddictClientActivationHandlingAsync(services =>
            {
                services.AddDbContext<DbContext>(options =>
                {
                    options.UseSqlite($"Filename={Path.Combine(Path.GetTempPath(), "openiddict-sandbox-uno-client.sqlite3")}");
                    options.UseOpenIddict();
                });

                services.AddOpenIddict()

                    // Register the OpenIddict core components.
                    .AddCore(options =>
                    {
                        // Configure OpenIddict to use the Entity Framework Core stores and models.
                        // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                        options.UseEntityFrameworkCore()
                               .UseDbContext<DbContext>();
                    })

                    // Register the OpenIddict client components.
                    .AddClient(options =>
                    {
                        // Note: this sample uses the authorization code and refresh token
                        // flows, but you can enable the other flows if necessary.
                        options.AllowAuthorizationCodeFlow()
                               .AllowRefreshTokenFlow();

                        // Register the signing and encryption credentials used to protect
                        // sensitive data like the state tokens produced by OpenIddict.
                        options.AddDevelopmentEncryptionCertificate()
                               .AddDevelopmentSigningCertificate();

                        //options.UseSystemIntegration();
                        options.UseUnoIntegration()
#if ANDROID || IOS
                            .DisableEmbeddedWebServer()
                            .DisablePipeServer()
#endif
                        ;

                        // Register the System.Net.Http integration and use the identity of the current
                        // assembly as a more specific user agent, which can be useful when dealing with
                        // providers that use the user agent as a way to throttle requests (e.g Reddit).
                        options.UseSystemNetHttp()
                               .SetProductInformation(typeof(App).Assembly);

                        // Add a client registration matching the client application definition in the server project.
                        options.AddRegistration(new OpenIddictClientRegistration
                        {
#if ANDROID
                            Issuer = new Uri("https://10.0.2.2:44395/", UriKind.Absolute),
#else
                            Issuer = new Uri("https://localhost:44395/", UriKind.Absolute),
#endif
                            ProviderName = "Local",

                            ClientId = "uno",

                            // This sample uses protocol activations with a custom URI scheme to handle callbacks.
                            //
                            // For more information on how to construct private-use URI schemes,
                            // read https://www.rfc-editor.org/rfc/rfc8252#section-7.1 and
                            // https://www.rfc-editor.org/rfc/rfc7595#section-3.8.
                            PostLogoutRedirectUri = new Uri("com.openiddict.sandbox.uno.client:/callback/logout/local", UriKind.Absolute),
                            RedirectUri = new Uri("com.openiddict.sandbox.uno.client:/callback/login/local", UriKind.Absolute),

                            Scopes = { Scopes.Email, Scopes.Profile, Scopes.OfflineAccess, "demo_api" }
                        });

                        // Register the Web providers integrations.
                        //
                        // Note: to mitigate mix-up attacks, it's recommended to use a unique redirection endpoint
                        // address per provider, unless all the registered providers support returning an "iss"
                        // parameter containing their URL as part of authorization responses. For more information,
                        // see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics#section-4.4.
                        options.UseWebProviders()
                               .AddGitHub(options =>
                               {
                                   options.SetClientId("8abc54b6d5f4e39d78aa")
                                          .SetClientSecret("f37ef38bdb18a0f5f2d430a8edbed4353c012dc3")
                                          // Note: GitHub doesn't support the recommended ":/" syntax and requires using "://".
                                          .SetRedirectUri("com.openiddict.sandbox.uno.client://callback/login/github");
                               });
                    });

                // Register the worker responsible for creating the database used to store tokens
                // and adding the registry entries required to register the custom URI scheme.
                //
                // Note: in a real world application, this step should be part of a setup script.
                services.AddHostedService<Worker>();
            },
            "OpenIddict.Sandbox.Uno.Client");
        builder
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                // Register Json serializers (ISerializer and ISerializer)
                .UseSerialization((context, services) => services
                    .AddContentSerializer(context)
                    .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                .UseHttp((context, services) => services
                    // Register HttpClient
#if DEBUG
                    // DelegatingHandler will be automatically injected into Refit Client
                    .AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
                    .AddSingleton<IWeatherCache, WeatherCache>()
                    .AddRefitClient<IApiClient>(context))
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IHostApplicationLifetime, UnoHostApplicationLifetime>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.EnableHotReload();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>(async (sp, navigator) => {
            await navigator.NavigateViewModelAsync<LoginViewModel>(this);
        });
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<LoginPage, LoginViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Login", View: views.FindByViewModel<LoginViewModel>()),
                ]
            )
        );
    }
}
