using Newtonsoft.Json;
using Fig.Client.Testing.Integration;

namespace Fig.Examples.AspNetApi.Test
{
    public class IntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task ShallReturnDefaultLocation()
        {
            var response = await Client!.GetStringAsync("WeatherForecast");

            var forecast = JsonConvert.DeserializeObject<List<WeatherForecast>>(response);

            Assert.That(forecast?.First().Location, Is.EqualTo(Settings.Location));
        }

        [TestCase("London")]
        [TestCase("Paris")]
        [TestCase("New York")]
        public async Task ShallReturnUpdatedLocation(string locationName)
        {
            Settings.Location = locationName;
            ConfigReloader.Reload(Settings);

            var response = await Client!.GetStringAsync("WeatherForecast");

            var forecast = JsonConvert.DeserializeObject<List<WeatherForecast>>(response);

            Assert.That(forecast?.First().Location, Is.EqualTo(locationName));
        }

        [Test]
        public async Task ShallVerifySettingsAreBoundToOptionsMonitor()
        {
            await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
                Application!.Services,
                ConfigReloader,
                Settings,
                settings => settings.Location = $"Verifier-{Guid.NewGuid():N}",
                settings => settings.Location);
        }
    }
}