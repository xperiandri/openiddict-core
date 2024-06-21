namespace OpenIddict.Sandbox.Uno.Client.Services.Caching;

public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
