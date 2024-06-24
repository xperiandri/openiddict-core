/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */
#if !WINDOWS
using System.ComponentModel;

using OpenIddict.Extensions;

namespace OpenIddict.Client.UnoIntegration;

/// <summary>
/// Represents a protocol activation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class OpenIddictClientUnoIntegrationActivation
{
    /// <summary>
    /// Creates a new instance of the <see cref="OpenIddictClientUnoIntegrationActivation"/> class.
    /// </summary>
    public OpenIddictClientUnoIntegrationActivation()
    {
    }

    /// <summary>
    /// Gets or sets a boolean indicating whether the activation
    /// was redirected from another instance of the application.
    /// </summary>
    public bool IsActivationRedirected { get; set; }
}
#endif