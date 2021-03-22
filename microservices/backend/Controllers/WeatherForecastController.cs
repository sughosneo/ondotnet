using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using Microsoft.FeatureManagement;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IFeatureManager _featureManager;
        private readonly ILogger<WeatherForecastController> _logger;
        private ActivitySource _activitySource = new ActivitySource(nameof(WeatherForecastController));
        private static readonly TextMapPropagator Propagator = new TraceContextPropagator();
        private static readonly Func<IDictionary<string, string>, string, IEnumerable<string>> _getter;
        private IAzureMapWeatherSvc _azureMapWeatherSvc;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IFeatureManager featureManager, IAzureMapWeatherSvc azureMapWeatherSvc)
        {
            _logger = logger;
            _featureManager = featureManager;
            _azureMapWeatherSvc = azureMapWeatherSvc;
        }

        [HttpGet]
        public async Task<string> Get()
        {

            var carrier = new Dictionary<string, string>();
            var parentContext = Propagator.Extract(default, carrier, _getter);
            Baggage.Current = parentContext.Baggage;

            using (var activity = _activitySource.StartActivity("GET:Recevied", ActivityKind.Server))
            {
                try
                {
                    _logger.LogInformation("{Method} - was called ", "backend.Controllers.WeatherForecastController.Get");

                    _activitySource.StartActivity("Get()");

                    string weather = string.Empty;

                    if (await _featureManager.IsEnabledAsync("ExternalWeatherAPI"))
                    {
                        _logger.LogInformation("Calling external api");
                        weather = await GetWeatherExternalData();
                    }
                    else
                    {
                        _logger.LogInformation("Fetching static data");
                        weather = await GetWeatherStaticData();
                    }

                    _logger.LogInformation("Weather data - {data}", weather);

                    activity?.SetTag("weather-data", weather);

                    _logger.LogInformation("Weather data fetched !");

                    return weather;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unable to fetch weather data !", ex);
                    throw;
                }
            }
        }

        private async Task<string> GetWeatherStaticData()
        {
            var rng = new Random();
            var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

            return JsonSerializer.Serialize(forecasts);
        }

        private async Task<string> GetWeatherExternalData()
        {
            var modifiedWeatherData = await _azureMapWeatherSvc.GetAzureMapWeatherForcastDate();
            return JsonSerializer.Serialize(modifiedWeatherData);
        }
    }
}
