namespace OpenIddict.Sandbox.UnoClient.Services.Caching;

public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
