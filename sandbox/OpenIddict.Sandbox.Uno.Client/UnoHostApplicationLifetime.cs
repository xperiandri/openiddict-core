using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace OpenIddict.Sandbox.UnoClient;

public class UnoHostApplicationLifetime : ApplicationLifetime, IHostApplicationLifetime
{
    public UnoHostApplicationLifetime(ILogger<ApplicationLifetime> logger) : base(logger) { }

    CancellationToken IHostApplicationLifetime.ApplicationStarted => this.ApplicationStarted;

    CancellationToken IHostApplicationLifetime.ApplicationStopping => this.ApplicationStopping;

    CancellationToken IHostApplicationLifetime.ApplicationStopped => this.ApplicationStopped;

    void IHostApplicationLifetime.StopApplication()
    {
        this.StopApplication();
        Environment.Exit(0);
    }
}