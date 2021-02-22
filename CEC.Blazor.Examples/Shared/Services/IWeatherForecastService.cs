using CEC.Blazor.Examples.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Examples.Services
{
    public interface IWeatherForecastService
    {
        public Task<WeatherForecast[]> GetForecastAsync();
    }
}
