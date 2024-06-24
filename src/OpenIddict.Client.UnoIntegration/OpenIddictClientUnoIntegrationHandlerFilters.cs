/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

using static OpenIddict.Client.OpenIddictClientEvents;
using OpenIddict.Client.SystemIntegration;

namespace OpenIddict.Client.UnoIntegration;

/// <summary>
/// Contains a collection of event handler filters commonly used by the system integration handlers.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class OpenIddictClientUnoIntegrationHandlerFilters
{
    /// <summary>
    /// Represents a filter that excludes the associated handlers if no protocol activation was found.
    /// </summary>
    public sealed class RequireProtocolActivation : IOpenIddictClientHandlerFilter<BaseContext>
    {
        /// <inheritdoc/>
        public ValueTask<bool> IsActiveAsync(BaseContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new(context.Transaction.GetProtocolActivation() is not null);
        }
    }

//    /// <summary>
//    /// Represents a filter that excludes the associated handlers if
//    /// the web authentication broker integration was not enabled.
//    /// </summary>
//    public sealed class RequireWebAuthenticationBroker : IOpenIddictClientHandlerFilter<BaseContext>
//    {
//        private readonly IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> _options;

//        public RequireWebAuthenticationBroker(IOptionsMonitor<OpenIddictClientSystemIntegrationOptions> options)
//            => _options = options ?? throw new ArgumentNullException(nameof(options));

//        /// <inheritdoc/>
//        public ValueTask<bool> IsActiveAsync(BaseContext context)
//        {
//            if (context is null)
//            {
//                throw new ArgumentNullException(nameof(context));
//            }

//#if SUPPORTS_WINDOWS_RUNTIME
//            if (OpenIddictClientSystemIntegrationHelpers.IsWebAuthenticationBrokerSupported())
//            {
//                if (!context.Transaction.Properties.TryGetValue(
//                    typeof(OpenIddictClientSystemIntegrationAuthenticationMode).FullName!, out var result) ||
//                    result is not OpenIddictClientSystemIntegrationAuthenticationMode mode)
//                {
//                    mode = _options.CurrentValue.AuthenticationMode.GetValueOrDefault();
//                }

//                return new(mode is OpenIddictClientSystemIntegrationAuthenticationMode.WebAuthenticationBroker);
//            }
//#endif
//            return new(false);
//        }
//    }

//    /// <summary>
//    /// Represents a filter that excludes the associated handlers if no
//    /// web authentication operation was triggered during the transaction.
//    /// </summary>
//    public sealed class RequireWebAuthenticationResult : IOpenIddictClientHandlerFilter<BaseContext>
//    {
//        /// <inheritdoc/>
//        public ValueTask<bool> IsActiveAsync(BaseContext context)
//        {
//            if (context is null)
//            {
//                throw new ArgumentNullException(nameof(context));
//            }

//#if SUPPORTS_WINDOWS_RUNTIME
//            if (OpenIddictClientSystemIntegrationHelpers.IsWebAuthenticationBrokerSupported())
//            {
//                return new(ContainsWebAuthenticationResult(context.Transaction));
//            }

//            [MethodImpl(MethodImplOptions.NoInlining)]
//            static bool ContainsWebAuthenticationResult(OpenIddictClientTransaction transaction)
//                => transaction.GetWebAuthenticationResult() is not null;
//#endif
//            return new(false);
//        }
//    }
}
