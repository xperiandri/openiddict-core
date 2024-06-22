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
public sealed class OpenIddictClientUnoIntegrationConfiguration : IPostConfigureOptions<OpenIddictClientSystemIntegrationOptions>
{
    /// <summary>
    /// Creates a new instance of the <see cref="OpenIddictClientSystemIntegrationConfiguration"/> class.
    /// </summary>
    public OpenIddictClientUnoIntegrationConfiguration() { }

    /// <inheritdoc/>
    public void PostConfigure(string? name, OpenIddictClientSystemIntegrationOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        OpenIddictClientUnoIntegrationHandlers.RedirectProtocolActivation.Identifier = options.InstanceIdentifier;
    }
}
