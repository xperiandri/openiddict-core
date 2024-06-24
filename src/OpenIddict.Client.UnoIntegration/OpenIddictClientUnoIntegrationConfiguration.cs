/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.ComponentModel;
using System.IO.Pipes;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Extensions;
using OpenIddict.Client.SystemIntegration;

#if !SUPPORTS_HOST_ENVIRONMENT
using IHostEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
#endif

namespace OpenIddict.Client.UnoIntegration;

/// <summary>
/// Contains the methods required to ensure that the OpenIddict client system integration configuration is valid.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class OpenIddictClientUnoIntegrationConfiguration : IConfigureOptions<OpenIddictClientOptions>,
                                                                  IPostConfigureOptions<OpenIddictClientOptions>,
                                                                  IPostConfigureOptions<OpenIddictClientSystemIntegrationOptions>
{
    private readonly OpenIddictClientSystemIntegrationConfiguration _systemIntegrationConfiguration;

    /// <summary>
    /// Creates a new instance of the <see cref="OpenIddictClientUnoIntegrationConfiguration"/> class.
    /// </summary>
    /// <param name="environment">The host environment.</param>
    public OpenIddictClientUnoIntegrationConfiguration(IHostEnvironment environment)
        => _systemIntegrationConfiguration = new OpenIddictClientSystemIntegrationConfiguration(environment ?? throw new ArgumentNullException(nameof(environment)));

    /// <inheritdoc/>
    public void Configure(OpenIddictClientOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Register the built-in event handlers used by the OpenIddict client system integration components.
        options.Handlers.AddRange(OpenIddictClientUnoIntegrationHandlers.DefaultHandlers);
    }

    /// <inheritdoc/>
    public void PostConfigure(string? name, OpenIddictClientOptions options)
    {
        _systemIntegrationConfiguration.PostConfigure(name, options);
    }

    /// <inheritdoc/>
    public void PostConfigure(string? name, OpenIddictClientSystemIntegrationOptions options)
    {
        _systemIntegrationConfiguration.PostConfigure(name, options);
#if WINDOWS
        OpenIddictClientUnoIntegrationHandlers.RedirectProtocolActivation.Identifier = options.InstanceIdentifier;
#endif
    }
}
