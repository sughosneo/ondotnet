﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

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

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        //  [HttpGet]
        //  public IEnumerable<WeatherForecast> Get()
        //  {
        //      var rng = new Random();
        //      return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //      {
        //          Date = DateTime.Now.AddDays(index),
        //          TemperatureC = rng.Next(-20, 55),
        //          Summary = Summaries[rng.Next(Summaries.Length)]
        //      })
        //      .ToArray();
        //  }

        [HttpGet]
        public async Task<string> Get([FromServices] IDistributedCache cache)
        {
            _logger.LogInformation("{Method} - was called ", "backend.Controllers.WeatherForecastController.Get");

            /*
            if (new Random().Next(50) < 20)
            {
                _logger.LogError("System is down!");
                throw new Exception("System is down");
            }*/

            try
            {
                var weather = await cache.GetStringAsync("weather");

                if (weather == null)
                {
                    var rng = new Random();
                    var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                    {
                        Date = DateTime.Now.AddDays(index),
                        TemperatureC = rng.Next(-20, 55),
                        Summary = Summaries[rng.Next(Summaries.Length)]
                    })
                    .ToArray();

                    weather = JsonSerializer.Serialize(forecasts);

                    _logger.LogInformation("Weather data - {data}", weather);

                    await cache.SetStringAsync("weather", weather, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
                    });
                }

                _logger.LogInformation("Weather data fetched !");

                return weather;
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to fetch result!", ex);
                throw;
            }
        }
    }
}
