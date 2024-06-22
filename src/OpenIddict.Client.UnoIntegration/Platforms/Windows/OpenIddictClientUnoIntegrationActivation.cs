/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.ComponentModel;
using Microsoft.Windows.AppLifecycle;

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
    /// <param name="activatedEventArgs">The app activation arguments with protocol activation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activatedEventArgs"/> is <see langword="null"/>.</exception>
    public OpenIddictClientUnoIntegrationActivation(AppActivationArguments activatedEventArgs)
    {
        if (activatedEventArgs is null)
        {
            throw new ArgumentNullException(nameof(activatedEventArgs));
        }

        if (activatedEventArgs.Kind != ExtendedActivationKind.Protocol)
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0144), nameof(activatedEventArgs));
        }

        AppActivationArguments = activatedEventArgs;
    }

    /// <summary>
    /// Gets the app activation arguments with protocol activation.
    /// </summary>
    public AppActivationArguments AppActivationArguments { get; }

    /// <summary>
    /// Gets or sets a boolean indicating whether the activation
    /// was redirected from another instance of the application.
    /// </summary>
    public bool IsActivationRedirected { get; set; }
}
