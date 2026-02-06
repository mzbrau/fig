using Fig.LoadTest;

var options = LoadTestOptions.Parse(args);
var definitions = LoadTestClientDefinition.CreateDefaultPairs();

using var runner = new LoadTestRunner(options, definitions);
await runner.RunAsync();