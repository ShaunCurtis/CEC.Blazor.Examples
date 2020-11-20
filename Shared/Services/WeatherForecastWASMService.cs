using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CEC.Blazor.Examples.Services
{
    public class WeatherForecastWASMService : WeatherForecastService
    {
        private HttpClient Http { get; set; } = null;

        public WeatherForecastWASMService( HttpClient http)
        {
            Http = http;
        }

        public async override Task<WeatherForecast[]> GetForecastAsync() => await Http.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
    }
}
