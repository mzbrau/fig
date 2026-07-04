using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

/// <summary>
/// Process-based E2E test: a sample app exports settings via --printappsettings,
/// then starts with --figoffline to validate settings are read correctly.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AppSettingsOfflineProcessTests
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(2);
    private static string _testHostProjectPath = null!;
    private static string _testHostOutputDir = null!;

    [OneTimeSetUp]
    public void BuildTestHost()
    {
        _testHostProjectPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "Fig.OfflineFlow.TestHost",
            "Fig.OfflineFlow.TestHost.csproj"));

        Assert.That(File.Exists(_testHostProjectPath), Is.True,
            $"Test host project not found at {_testHostProjectPath}");

        var buildResult = RunDotNet($"build \"{_testHostProjectPath}\"");
        Assert.That(buildResult.ExitCode, Is.EqualTo(0), buildResult.Output);

        _testHostOutputDir = Path.GetDirectoryName(_testHostProjectPath)!;
    }

    [Test]
    public void ShallExportAndLoadComplexSettingsThroughProcess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "fig-offline-e2e-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var printResult = RunTestHost(tempDir, "--printappsettings",
                "Service:Auth:Username=override-user",
                "Accounts:0:Password=override-alice-secret",
                "ApiKey=override-api-key");
            Assert.That(printResult.ExitCode, Is.EqualTo(0), printResult.Output);

            var generatedPath = Path.Combine(tempDir, "appsettings.fig.json");
            Assert.That(File.Exists(generatedPath), Is.True, "appsettings.fig.json should be generated");

            File.Move(generatedPath, Path.Combine(tempDir, "appsettings.json"));

            var loadResult = RunTestHost(tempDir, "--figoffline", "--dump-bound-settings");
            Assert.That(loadResult.ExitCode, Is.EqualTo(0), loadResult.Output);

            var boundPath = Path.Combine(tempDir, "bound-settings.json");
            Assert.That(File.Exists(boundPath), Is.True, "bound-settings.json should be written");

            var bound = JsonConvert.DeserializeObject<OfflineFlowTestHostSettings>(File.ReadAllText(boundPath));
            Assert.That(bound, Is.Not.Null);

            Assert.That(bound!.Service.Endpoint, Is.EqualTo("https://api.example.com"));
            Assert.That(bound.Service.Auth.Username, Is.EqualTo("override-user"));
            Assert.That(bound.Service.Auth.Token, Is.EqualTo("service-token"));
            Assert.That(bound.ApiKey, Is.EqualTo("override-api-key"));
            Assert.That(bound.Accounts, Has.Count.EqualTo(2));
            Assert.That(bound.Accounts[0].Username, Is.EqualTo("alice"));
            Assert.That(bound.Accounts[0].Password, Is.EqualTo("override-alice-secret"));
            Assert.That(bound.Accounts[1].Username, Is.EqualTo("bob"));
            Assert.That(bound.Accounts[1].Password, Is.EqualTo("bob-secret"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private static (int ExitCode, string Output) RunTestHost(string workingDirectory, params string[] hostArgs)
    {
        var args = new List<string>
        {
            "run",
            "--project", _testHostProjectPath,
            "--no-build",
            "--"
        };
        args.AddRange(hostArgs);

        return RunDotNet(string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)), workingDirectory);
    }

    private static (int ExitCode, string Output) RunDotNet(string arguments, string? workingDirectory = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? _testHostOutputDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)!;
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        var allTasks = Task.WhenAll(
            process.WaitForExitAsync(),
            standardOutputTask,
            standardErrorTask);

        if (!allTasks.Wait(ProcessTimeout))
        {
            TryKillProcessTree(process);
            try
            {
                allTasks.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // best effort; collect whatever output is available
            }

            var timedOutOutput = GetTaskResultOrEmpty(standardOutputTask) + GetTaskResultOrEmpty(standardErrorTask);
            return (-1, $"Process timed out after {ProcessTimeout.TotalMinutes:F0} minutes.{Environment.NewLine}{timedOutOutput}");
        }

        var output = standardOutputTask.Result + standardErrorTask.Result;
        return (process.ExitCode, output);
    }

    private static string GetTaskResultOrEmpty(Task<string> task)
    {
        return task.IsCompletedSuccessfully ? task.Result : string.Empty;
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch
        {
            // best effort cleanup
        }
    }
}
