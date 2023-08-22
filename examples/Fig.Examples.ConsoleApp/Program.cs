// See https://aka.ms/new-console-template for more information

using Fig.Client;
using Fig.Client.Configuration;
using Fig.Client.Factories;
using Fig.Client.Logging;
using Fig.Examples.ConsoleApp;

var figOptions = new FigOptions
{
    ApiUri = new Uri("http://localhost:5051"),
    ClientSecret = "c059383fc9b145d99b596bd00d892cf0"
};
var loggerFactory = new SimpleLoggerFactory();
var provider = FigConfigurationProvider.Create(loggerFactory, figOptions, new SimpleHttpClientFactory(figOptions.ApiUri));

IConsoleSettings settings = await provider.Initialize<ConsoleSettings>();

Console.WriteLine("Settings were:");
//Console.WriteLine(string.Join(",", settings.Items));
//Console.WriteLine(string.Join(",", settings.IntItems));
// Console.WriteLine($"Favourite Animal: {settings.FavouriteAnimal}");
// Console.WriteLine($"Favourite Number: {settings.FavouriteNumber}");
// Console.WriteLine($"True or False: {settings.TrueOrFalse}");
/*Console.WriteLine($"Pet: {settings.Pets}");
Console.WriteLine($"Fish: {settings.Fish}");
Console.WriteLine($"Aussie: {settings.AustralianAnimals}");
Console.WriteLine($"Swedish: {settings.SwedishAnimals}");*/

settings.RestartRequested += (sender, args) => { Console.WriteLine("Restart requested!"); };

// using var httpClient = new HttpClient();
// httpClient.BaseAddress = new Uri("https://localhost:7281");
//
// var auth = new AuthenticateRequestDataContract
// {
//     Username = "admin",
//     Password = "admin"
// };
//
// var authJson = JsonConvert.SerializeObject(auth);
// var authData = new StringContent(authJson, Encoding.UTF8, "application/json");
//
// var response = await httpClient.PostAsync("/users/authenticate", authData);
//
// var responseString = await response.Content.ReadAsStringAsync();
// var responseDataContract = JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(responseString);
//
//
// var item = new LookupTableDataContract()
// {
//     Name = "Animals2",
//     Enumeration = new Dictionary<string, string>()
//     {
//         { "1", "Dog" },
//         { "2", "Cat" },
//         { "3", "Fish" }
//     }
// };
//
// var json = JsonConvert.SerializeObject(item);
// var data = new StringContent(json, Encoding.UTF8, "application/json");
//
//
//
// httpClient.DefaultRequestHeaders.Add("Authorization", responseDataContract.Token);
// var result = await httpClient.PostAsync("/lookuptables", data);

settings.SettingsChanged += (sender, eventArgs) =>
{
    Console.WriteLine($"{DateTime.Now}: Settings have changed!");
    Console.WriteLine("Settings were:");
    //Console.WriteLine(string.Join(",", settings.Items));
    /*Console.WriteLine($"Pet: {settings.Pets}");
    Console.WriteLine($"Fish: {settings.Fish}");
    Console.WriteLine($"Aussie: {settings.AustralianAnimals}");*/
};

Console.ReadKey();