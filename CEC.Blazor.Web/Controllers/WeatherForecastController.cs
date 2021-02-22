using CEC.Blazor.Examples.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CEC.Blazor.Examples.Data;

namespace Blazor.Examples.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> logger;

        private IWeatherForecastService Service { get; set; }

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherForecastService weatherStationService)
        {
            this.logger = logger;
            Service = weatherStationService;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> GetForcastList() => await this.Service.GetForecastAsync();
    }
}
