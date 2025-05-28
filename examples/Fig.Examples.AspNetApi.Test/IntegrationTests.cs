using Newtonsoft.Json;

namespace Fig.Examples.AspNetApi.Test
{
    public class IntegrationTests : IntegrationTestBase
    {        [Test]
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
    }
}