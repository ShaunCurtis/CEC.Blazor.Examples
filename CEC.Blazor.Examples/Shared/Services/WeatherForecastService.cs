using CEC.Blazor.Examples.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Examples.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public virtual Task<WeatherForecast[]> GetForecastAsync()
        {
            var rng = new Random();
            var startDate = DateTime.Now.AddDays(-14);
            var index = 14;
            return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray());
        }
    }
}
