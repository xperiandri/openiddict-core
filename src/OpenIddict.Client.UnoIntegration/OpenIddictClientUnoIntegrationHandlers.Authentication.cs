/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Primitives;
using OpenIddict.Extensions;

#if SUPPORTS_WINDOWS_RUNTIME
using Windows.Security.Authentication.Web;
using Windows.UI.Core;
#endif

using static OpenIddict.Client.OpenIddictClientEvents;
using OpenIddict.Client.SystemIntegration;
using static OpenIddict.Client.UnoIntegration.OpenIddictClientUnoIntegrationHandlerFilters;
using Windows.ApplicationModel.Activation;

namespace OpenIddict.Client.UnoIntegration;

public static partial class OpenIddictClientUnoIntegrationHandlers
{
    public static class Authentication
    {
        public static ImmutableArray<OpenIddictClientHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray.Create([
            /*
             * Authorization request processing:
             */
            OpenIddictClientSystemIntegrationHandlers.Authentication.InvokeWebAuthenticationBroker.Descriptor,
            OpenIddictClientSystemIntegrationHandlers.Authentication.LaunchSystemBrowser.Descriptor,

            /*
             * Redirection request extraction:
             */
            OpenIddictClientSystemIntegrationHandlers.ExtractGetOrPostHttpListenerRequest<ExtractRedirectionRequestContext>.Descriptor,
            ExtractProtocolActivationParameters<ExtractRedirectionRequestContext>.Descriptor,
            OpenIddictClientSystemIntegrationHandlers.ExtractWebAuthenticationResultData<ExtractRedirectionRequestContext>.Descriptor,

            /*
             * Redirection response handling:
             */
            OpenIddictClientSystemIntegrationHandlers.AttachHttpResponseCode<ApplyRedirectionResponseContext>.Descriptor,
            OpenIddictClientSystemIntegrationHandlers.AttachCacheControlHeader<ApplyRedirectionResponseContext>.Descriptor,
            OpenIddictClientSystemIntegrationHandlers.Authentication.ProcessEmptyHttpResponse.Descriptor,
            ProcessProtocolActivationResponse<ApplyRedirectionResponseContext>.Descriptor,
            OpenIddictClientSystemIntegrationHandlers.ProcessWebAuthenticationResultResponse<ApplyRedirectionResponseContext>.Descriptor
        ]);

        /// <summary>
        /// Contains the logic responsible for extracting OpenID Connect requests
        /// from the URI of an initial or redirected protocol activation.
        /// Note: this handler is not used when the OpenID Connect request is not a protocol activation.
        /// </summary>
        public sealed class ExtractProtocolActivationParameters<TContext> : IOpenIddictClientHandler<TContext> where TContext : BaseValidatingContext
        {
            /// <summary>
            /// Gets the default descriptor definition assigned to this handler.
            /// </summary>
            public static OpenIddictClientHandlerDescriptor Descriptor { get; }
                = OpenIddictClientHandlerDescriptor.CreateBuilder<TContext>()
                    .AddFilter<RequireProtocolActivation>()
                    .UseSingletonHandler<ExtractProtocolActivationParameters<TContext>>()
                    .SetOrder(OpenIddictClientSystemIntegrationHandlers.ExtractGetOrPostHttpListenerRequest<TContext>.Descriptor.Order + 1_000)
                    .SetType(OpenIddictClientHandlerType.BuiltIn)
                    .Build();

            /// <inheritdoc/>
            public ValueTask HandleAsync(TContext context)
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                context.Transaction.Request = context.Transaction.GetProtocolActivation() switch
                {
#if WINDOWS
                    { AppActivationArguments.Data: IProtocolActivatedEventArgs protocolArgs } => new OpenIddictRequest(OpenIddictHelpers.ParseQuery(protocolArgs.Uri.Query)),
#else
                    { } => throw new NotImplementedException(),
#endif
                    _ => throw new InvalidOperationException(SR.GetResourceString(SR.ID0375))
                };

                return default;
            }
        }

        /// <summary>
        /// Contains the logic responsible for marking OpenID Connect
        /// responses returned via protocol activations as processed.
        /// </summary>
        public sealed class ProcessProtocolActivationResponse<TContext> : IOpenIddictClientHandler<TContext>
            where TContext : BaseRequestContext
        {
            /// <summary>
            /// Gets the default descriptor definition assigned to this handler.
            /// </summary>
            public static OpenIddictClientHandlerDescriptor Descriptor { get; }
                = OpenIddictClientHandlerDescriptor.CreateBuilder<TContext>()
                    .AddFilter<RequireProtocolActivation>()
                    .UseSingletonHandler<ProcessProtocolActivationResponse<TContext>>()
                    .SetOrder(OpenIddictClientSystemIntegrationHandlers.ProcessWebAuthenticationResultResponse<TContext>.Descriptor.Order - 1_000)
                    .SetType(OpenIddictClientHandlerType.BuiltIn)
                    .Build();

            /// <inheritdoc/>
            public ValueTask HandleAsync(TContext context)
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                // For both protocol activations (initial or redirected) and web-view-like results,
                // no proper response can be generated and eventually displayed to the user. In this
                // case, simply stop processing the response and mark the request as fully handled.
                //
                // Note: this logic applies to both successful and errored responses.

                context.HandleRequest();
                return default;
            }
        }


    }
}
