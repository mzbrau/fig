using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using NUnit.Framework;

namespace Fig.Api.SettingVerification.Unit.Test;

public class SettingDynamicVerificationRunnerTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task ShallRunBasicVerification()
    {
        var code = @"using System.Collections.Generic;
using Fig.Contracts.SettingVerification;
namespace Fig.Api.SettingVerification.Unit.Test;
public class BasicVerification: ISettingVerification
{
    public VerificationResultDataContract PerformVerification(Dictionary<string, object> settingValues)
    {
        return new VerificationResultDataContract()
        {
            Success = true,
            Message = ""The test was successful.""
        };
    }
}";
        var dataContract = new SettingVerificationDefinitionDataContract()
        {
            Code = code,
            Name = "BasicTest",
            Description = "This is a desc.",
            TargetRuntime = TargetRuntime.Dotnet6
        };

        var runner = new SettingDynamicVerificationRunner();
        var result = await runner.Run(dataContract, Array.Empty<SettingDefinitionDataContract>());
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("The test was successful."));
    }
    
    [Test]
    public async Task ShallHandleExceptionInExecutingCode()
    {
        var code = @"using System;
using System.Collections.Generic;
using Fig.Contracts.SettingVerification;
namespace Fig.Api.SettingVerification.Unit.Test;
public class BasicVerification: ISettingVerification
{
    public VerificationResultDataContract PerformVerification(Dictionary<string, object> settingValues)
    {
        throw new Exception(""No good"");
    }
}";
        var dataContract = new SettingVerificationDefinitionDataContract()
        {
            Code = code,
            Name = "BasicTest",
            Description = "This is a desc.",
            TargetRuntime = TargetRuntime.Dotnet6
        };

        var runner = new SettingDynamicVerificationRunner();
        var result = await runner.Run(dataContract, Array.Empty<SettingDefinitionDataContract>());
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message.StartsWith("Exception during code execution"));
        Assert.That(result.Message.Contains("No good"));
    }
    
    [Test]
    public async Task ShallHandleCompileErrorInProvidedCode()
    {
        var codeMissingUsings = @"
namespace Fig.Api.SettingVerification.Unit.Test;
public class BasicVerification: ISettingVerification
{
    public VerificationResultDataContract PerformVerification(Dictionary<string, object> settingValues)
    {
        return new VerificationResultDataContract();
    }
}";
        var dataContract = new SettingVerificationDefinitionDataContract()
        {
            Code = codeMissingUsings,
            Name = "BasicTest",
            Description = "This is a desc.",
            TargetRuntime = TargetRuntime.Dotnet6
        };

        var runner = new SettingDynamicVerificationRunner();
        var result = await runner.Run(dataContract, Array.Empty<SettingDefinitionDataContract>());
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message.StartsWith("Compile Error, see logs for details"));
        Assert.That(result.Logs.Count(), Is.EqualTo(4));
    }
}