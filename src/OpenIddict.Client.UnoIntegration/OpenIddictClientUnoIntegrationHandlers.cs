/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Claims;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenIddict.Extensions;

using static OpenIddict.Client.OpenIddictClientEvents;
using static OpenIddict.Client.OpenIddictClientHandlers;
using static OpenIddict.Client.OpenIddictClientHandlerFilters;

using OpenIddict.Client.SystemIntegration;

//using static OpenIddict.Client.SystemIntegration.OpenIddictClientSystemIntegrationHandlers;
//using static OpenIddict.Client.SystemIntegration.OpenIddictClientSystemIntegrationHandlerFilters;
using static OpenIddict.Client.UnoIntegration.OpenIddictClientUnoIntegrationHandlerFilters;

#if WINDOWS
using Microsoft.Windows.AppLifecycle;

#endif
using Windows.ApplicationModel.Activation;
using static OpenIddict.Client.UnoIntegration.OpenIddictClientUnoIntegrationHandlers;



#if !SUPPORTS_HOST_APPLICATION_LIFETIME
using IHostApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;
#endif

#if SUPPORTS_WINDOWS_RUNTIME
using Windows.Security.Authentication.Web;
#endif

namespace OpenIddict.Client.UnoIntegration;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class OpenIddictClientUnoIntegrationHandlers
{
    public static ImmutableArray<OpenIddictClientHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray.Create([
        /*
         * Top-level request processing:
         */
        OpenIddictClientSystemIntegrationHandlers.ResolveRequestUriFromHttpListenerRequest.Descriptor,
        ResolveRequestUriFromProtocolActivation.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.ResolveRequestUriFromWebAuthenticationResult.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.InferEndpointTypeFromDynamicAddress.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RejectUnknownHttpRequests.Descriptor,

        /*
         * Authentication processing:
         */
        OpenIddictClientSystemIntegrationHandlers.WaitMarshalledAuthentication.Descriptor,

        OpenIddictClientSystemIntegrationHandlers.RestoreClientRegistrationFromMarshalledContext.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreStateTokenFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreStateTokenPrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreHostAuthenticationPropertiesFromMarshalledAuthentication.Descriptor,

        RedirectProtocolActivation.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.ResolveRequestForgeryProtection.Descriptor,

        OpenIddictClientSystemIntegrationHandlers.RestoreFrontchannelTokensFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreFrontchannelIdentityTokenPrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreFrontchannelAccessTokenPrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreAuthorizationCodePrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreTokenResponseFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreBackchannelTokensFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreBackchannelIdentityTokenPrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreBackchannelAccessTokenPrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreRefreshTokenPrincipalFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreUserinfoDetailsFromMarshalledAuthentication.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.RestoreMergedPrincipalFromMarshalledAuthentication.Descriptor,

        OpenIddictClientSystemIntegrationHandlers.CompleteAuthenticationOperation.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.UntrackMarshalledAuthenticationOperation.Descriptor,

        /*
         * Challenge processing:
         */
        OpenIddictClientSystemIntegrationHandlers.InferBaseUriFromClientUri.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.AttachDynamicPortToRedirectUri.Descriptor,
        AttachInstanceIdentifier.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.TrackAuthenticationOperation.Descriptor,

        /*
         * Sign-out processing:
         */
        OpenIddictClientSystemIntegrationHandlers.InferLogoutBaseUriFromClientUri.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.AttachDynamicPortToPostLogoutRedirectUri.Descriptor,
        AttachLogoutInstanceIdentifier.Descriptor,
        OpenIddictClientSystemIntegrationHandlers.TrackLogoutOperation.Descriptor,

        /*
         * Error processing:
         */
        OpenIddictClientSystemIntegrationHandlers.AbortAuthenticationDemand.Descriptor,

        .. OpenIddictClientUnoIntegrationHandlers.Authentication.DefaultHandlers,
        .. OpenIddictClientSystemIntegrationHandlers.Session.DefaultHandlers,
    ]);

    /// <summary>
    /// Contains the logic responsible for resolving the request URI from the protocol activation details.
    /// Note: this handler is not used when the OpenID Connect request is not a protocol activation.
    /// </summary>
    public sealed class ResolveRequestUriFromProtocolActivation : IOpenIddictClientHandler<ProcessRequestContext>
    {
        /// <summary>
        /// Gets the default descriptor definition assigned to this handler.
        /// </summary>
        public static OpenIddictClientHandlerDescriptor Descriptor { get; }
            = OpenIddictClientHandlerDescriptor.CreateBuilder<ProcessRequestContext>()
                .AddFilter<RequireProtocolActivation>()
                .UseSingletonHandler<ResolveRequestUriFromProtocolActivation>()
                .SetOrder(OpenIddictClientSystemIntegrationHandlers.ResolveRequestUriFromHttpListenerRequest.Descriptor.Order + 1_000)
                .SetType(OpenIddictClientHandlerType.BuiltIn)
                .Build();

        /// <inheritdoc/>
        public ValueTask HandleAsync(ProcessRequestContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            (context.BaseUri, context.RequestUri) = context.Transaction.GetProtocolActivation() switch
            {
#if WINDOWS
                { AppActivationArguments.Data: IProtocolActivatedEventArgs protocolArgs }
                    => (
                    BaseUri: new UriBuilder(protocolArgs.Uri) { Path = null, Query = null, Fragment = null }.Uri,
                    RequestUri: protocolArgs.Uri),
#else
                // TODO: Check for other platforms
                {  } => (
                    BaseUri: new Uri(""),
                    RequestUri: new Uri("")),
                //{ ActivationUri: Uri uri } => (
                //    BaseUri: new UriBuilder(uri) { Path = null, Query = null, Fragment = null }.Uri,
                //    RequestUri: uri),
#endif

                _ => throw new InvalidOperationException(SR.GetResourceString(SR.ID0375))
            };

            return default;
        }
    }

    /// <summary>
    /// Contains the logic responsible for redirecting the protocol activation to
    /// the instance that initially started the authentication demand, if applicable.
    /// Note: this handler is not used when the OpenID Connect request is not a protocol activation.
    /// </summary>
    public sealed class RedirectProtocolActivation : IOpenIddictClientHandler<ProcessAuthenticationContext>
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> _options;
        private readonly OpenIddictClientUnoIntegrationService _service;

        public RedirectProtocolActivation(
            IHostApplicationLifetime lifetime,
            IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> options,
            OpenIddictClientUnoIntegrationService service)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Gets the default descriptor definition assigned to this handler.
        /// </summary>
        public static OpenIddictClientHandlerDescriptor Descriptor { get; }
            = OpenIddictClientHandlerDescriptor.CreateBuilder<ProcessAuthenticationContext>()
                .AddFilter<RequireProtocolActivation>()
                .AddFilter<RequireStateTokenPrincipal>()
                .UseSingletonHandler<RedirectProtocolActivation>()
                .SetOrder(ResolveNonceFromStateToken.Descriptor.Order + 500)
                .SetType(OpenIddictClientHandlerType.BuiltIn)
                .Build();

        /// <inheritdoc/>
        public async ValueTask HandleAsync(ProcessAuthenticationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Debug.Assert(context.StateTokenPrincipal is { Identity: ClaimsIdentity }, SR.GetResourceString(SR.ID4006));

            var activation = context.Transaction.GetProtocolActivation() ??
                 throw new InvalidOperationException(SR.GetResourceString(SR.ID0375));

            var identifier = context.StateTokenPrincipal.GetClaim(Claims.Private.InstanceId);
            if (string.IsNullOrEmpty(identifier))
            {
                throw new InvalidOperationException(SR.GetResourceString(SR.ID0376));
            }

            // If the identifier stored in the state token doesn't match the identifier of the
            // current instance, stop processing the authentication demand in this process and
            // redirect the protocol activation to the correct instance. Once the redirection
            // has been received by the other instance, ask the host to stop the application.

            if (string.Equals(identifier, _options.CurrentValue.InstanceIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // If protocol activation redirection was not enabled, reject the request
            // as there's no additional processing that can be made at this stage.
            if (_options.CurrentValue.EnableActivationRedirection is not true)
            {
                context.Reject(
                    error: Errors.InvalidRequest,
                    description: SR.GetResourceString(SR.ID2166),
                    uri: SR.FormatID8000(SR.ID2166));

                return;
            }

            // Try to redirect the protocol activation to the correct instance.
            try
            {
                using var source = new CancellationTokenSource(delay: TimeSpan.FromSeconds(10));
                await _service.RedirectProtocolActivationAsync(activation, identifier, source.Token);
            }
            catch (Exception exception) when (!OpenIddictHelpers.IsFatal(exception))
            {
                context.Logger.LogWarning(SR.GetResourceString(SR.ID6215), identifier);
            }

            // Inform the host that the application should stop and mark the authentication context as handled
            // to prevent the other event handlers from being invoked while the application is shutting down.
            _lifetime.StopApplication();
            context.HandleRequest();
        }
    }

    /// <summary>
    /// Contains the logic responsible for storing the identifier of the current instance in the state token.
    /// Note: this handler is not used when the user session is not interactive.
    /// </summary>
    public sealed class AttachInstanceIdentifier : IOpenIddictClientHandler<ProcessChallengeContext>
    {
        private readonly IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> _options;

        public AttachInstanceIdentifier(IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> options)
            => _options = options ?? throw new ArgumentNullException(nameof(options));

        /// <summary>
        /// Gets the default descriptor definition assigned to this handler.
        /// </summary>
        public static OpenIddictClientHandlerDescriptor Descriptor { get; }
            = OpenIddictClientHandlerDescriptor.CreateBuilder<ProcessChallengeContext>()
                .AddFilter<OpenIddictClientSystemIntegrationHandlerFilters.RequireInteractiveSession>()
                .AddFilter<RequireLoginStateTokenGenerated>()
                .UseSingletonHandler<AttachInstanceIdentifier>()
                .SetOrder(PrepareLoginStateTokenPrincipal.Descriptor.Order + 500)
                .SetType(OpenIddictClientHandlerType.BuiltIn)
                .Build();

        /// <inheritdoc/>
        public ValueTask HandleAsync(ProcessChallengeContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Debug.Assert(context.StateTokenPrincipal is { Identity: ClaimsIdentity }, SR.GetResourceString(SR.ID4006));

            // Most applications (except Windows UWP applications) are multi-instanced. As such, any protocol activation
            // triggered by launching one of the URI schemes associated with the application will create a new instance,
            // different from the one that initially started the authentication flow. To deal with that without having to
            // share persistent state between instances, OpenIddict stores the identifier of the instance that starts the
            // authentication process and uses it when handling the callback to determine whether the protocol activation
            // should be redirected to a different instance using inter-process communication.
#if WINDOWS
            var instanceId = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(_options.CurrentValue.InstanceIdentifier).Key;

#else
            // TODO: Check for other platforms
            var instanceId = _options.CurrentValue.InstanceIdentifier;
#endif
            context.StateTokenPrincipal.SetClaim(Claims.Private.InstanceId, instanceId);

            return default;
        }
    }

    /// <summary>
    /// Contains the logic responsible for storing the identifier of the current instance in the state token.
    /// Note: this handler is not used when the user session is not interactive.
    /// </summary>
    public sealed class AttachLogoutInstanceIdentifier : IOpenIddictClientHandler<ProcessSignOutContext>
    {
        private readonly IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> _options;

        public AttachLogoutInstanceIdentifier(IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> options)
            => _options = options ?? throw new ArgumentNullException(nameof(options));

        /// <summary>
        /// Gets the default descriptor definition assigned to this handler.
        /// </summary>
        public static OpenIddictClientHandlerDescriptor Descriptor { get; }
            = OpenIddictClientHandlerDescriptor.CreateBuilder<ProcessSignOutContext>()
                .AddFilter<OpenIddictClientSystemIntegrationHandlerFilters.RequireInteractiveSession>()
                .AddFilter<RequireLogoutStateTokenGenerated>()
                .UseSingletonHandler<AttachLogoutInstanceIdentifier>()
                .SetOrder(PrepareLogoutStateTokenPrincipal.Descriptor.Order + 500)
                .SetType(OpenIddictClientHandlerType.BuiltIn)
                .Build();

        /// <inheritdoc/>
        public ValueTask HandleAsync(ProcessSignOutContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Debug.Assert(context.StateTokenPrincipal is { Identity: ClaimsIdentity }, SR.GetResourceString(SR.ID4006));

            // Most applications (except Windows UWP applications) are multi-instanced. As such, any protocol activation
            // triggered by launching one of the URI schemes associated with the application will create a new instance,
            // different from the one that initially started the logout flow. To deal with that without having to share
            // persistent state between instances, OpenIddict stores the identifier of the instance that starts the
            // logout process and uses it when handling the callback to determine whether the protocol activation
            // should be redirected to a different instance using inter-process communication.
#if WINDOWS
            var instanceId = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(_options.CurrentValue.InstanceIdentifier).Key;
#else
            // TODO: Check for other platforms
            var instanceId = _options.CurrentValue.InstanceIdentifier;
#endif
            context.StateTokenPrincipal.SetClaim(Claims.Private.InstanceId, instanceId);

            return default;
        }
    }
}