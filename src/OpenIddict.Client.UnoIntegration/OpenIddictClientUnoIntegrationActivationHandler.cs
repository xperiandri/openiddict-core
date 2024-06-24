/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
#if WINDOWS
using Microsoft.Windows.AppLifecycle;
#endif

using OpenIddict.Client.SystemIntegration;

namespace OpenIddict.Client.UnoIntegration;

/// <summary>
/// Contains the logic necessary to handle initial URI protocol activations.
/// </summary>
/// <remarks>
/// Note: redirected URI protocol activations are handled by <see cref="OpenIddictClientSystemIntegrationPipeListener"/>.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class OpenIddictClientUnoIntegrationActivationHandler : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> _options;
    private readonly IHostApplicationLifetime applicationLifetime;
    private readonly OpenIddictClientUnoIntegrationService _service;
    private bool _disposed;

    public OpenIddictClientUnoIntegrationActivationHandler(
        IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> options,
        IHostApplicationLifetime applicationLifetime,
        OpenIddictClientUnoIntegrationService service)
    {
        this.applicationLifetime = applicationLifetime;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _service = service ?? throw new ArgumentNullException(nameof(service));
#if WINDOWS
        if (_options.CurrentValue.EnableActivationHandling is true)
        {
            Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += CurrentAppInstance_Activated;
        }
#endif
    }

#if WINDOWS
    private async void CurrentAppInstance_Activated(object? sender, AppActivationArguments e)
        {
            if (e.Kind == ExtendedActivationKind.Protocol)
            {
                var activation = new OpenIddictClientUnoIntegrationActivation(e);
                await _service.HandleProtocolActivationAsync(activation);
            }
        }
#endif

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
#if WINDOWS
                Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated -= CurrentAppInstance_Activated;
#endif
            }

            _disposed = true;
        }
    }

    ~OpenIddictClientUnoIntegrationActivationHandler() => Dispose(false);

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Note: initial URI protocol activation handling is implemented as a regular IHostedService
        // rather than as a BackgroundService to allow blocking the initialization of the host until
        // the activation is fully processed by the OpenIddict pipeline. By doing that, the UI thread
        // is not started until redirection requests (like authorization responses) are fully processed,
        // which allows handling these requests transparently and helps avoid the "flashing window effect":
        // once a request has been handled by the OpenIddict pipeline, a dedicated handler is responsible
        // for stopping the application gracefully using the IHostApplicationLifetime.StopApplication() API.

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        // If the protocol activation processing logic was not enabled, ignore the activation.
        if (_options.CurrentValue.EnableActivationHandling is not true)
        {
            return Task.CompletedTask;
        }

        // Determine whether the current instance is initialized to react to a protocol activation.
        // If it's not, return immediately to avoid adding latency to the application startup process.
        if (GetProtocolActivation() is not OpenIddictClientUnoIntegrationActivation activation)
        {
            return Task.CompletedTask;
        }

        return _service.HandleProtocolActivationAsync(activation, cancellationToken);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static OpenIddictClientUnoIntegrationActivation? GetProtocolActivation()
        {
#if WINDOWS
            // On platforms that support WinRT, always favor the AppInstance.GetActivatedEventArgs() API.
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent()?.GetActivatedEventArgs();
            if (activatedEventArgs is not null && activatedEventArgs.Kind == ExtendedActivationKind.Protocol)
            {
                return new OpenIddictClientUnoIntegrationActivation(activatedEventArgs);
            }
#endif
            // Otherwise, try to extract the protocol activation from the command line arguments.
            // TODO: Handle other platforms
            //if (OpenIddictClientSystemIntegrationHelpers.GetProtocolActivationUriFromCommandLineArguments(
            //    Environment.GetCommandLineArgs()) is Uri value)
            //{
            //    return new OpenIddictClientSystemIntegrationActivation(value);
            //}

            return null;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
