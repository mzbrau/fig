using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Fig.Benchmarks.Test;

[MemoryDiagnoser]
public class RegexBenchmarks
{
    private static readonly Random random = new Random();
    private static readonly string[] randomStrings = new string[100];

    private static readonly string[] patterns = new string[]
    {
        @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}",
        @"(http|https)://[A-Za-z0-9.-]+\.[A-Za-z]{2,4}/?",
        @"\b\d{3}-\d{2}-\d{4}\b",
        @"\b\d{2}:\d{2}:\d{2}\b",
        @"\b(?:0?[1-9]|1[0-2]):[0-5][0-9](?:am|pm)\b"
    };

    private static readonly Regex[] regexInstances = new Regex[patterns.Length];

    [GlobalSetup]
    public void GlobalSetup()
    {
        for (int i = 0; i < randomStrings.Length; i++)
        {
            randomStrings[i] = GenerateRandomString();
        }

        for (int i = 0; i < patterns.Length; i++)
        {
            regexInstances[i] = new Regex(patterns[i]);
        }
    }

    [Benchmark]
    public void UsingIndividualRegexInstances()
    {
        foreach (var regex in regexInstances)
        {
            foreach (var input in randomStrings)
            {
                regex.IsMatch(input);
            }
        }
    }

    [Benchmark]
    public void UsingStaticIsMatch()
    {
        foreach (var pattern in patterns)
        {
            foreach (var input in randomStrings)
            {
                Regex.IsMatch(input, pattern);
            }
        }
    }

    private string GenerateRandomString()
    {
        int length = random.Next(20, 61);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}