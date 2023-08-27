// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Fig.Benchmarks.Test;

Console.WriteLine("Starting Benchmark");

//var summary = BenchmarkRunner.Run<RateMonitorBenchmarks>();
//var encryptionSummary = BenchmarkRunner.Run<EncryptionBenchmarks>();
var regexSummary = BenchmarkRunner.Run<RegexBenchmarks>();