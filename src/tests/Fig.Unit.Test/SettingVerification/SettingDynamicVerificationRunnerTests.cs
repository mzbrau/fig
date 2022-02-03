using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Api.SettingVerification.Dynamic;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.SettingVerification;

public class SettingDynamicVerificationRunnerTests
{
    private readonly Mock<ICodeHasher> _codeHasherMock = new();

    [SetUp]
    public void Setup()
    {
        _codeHasherMock.Setup(a => a.IsValid(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
    }

    [Test]
    public async Task ShallRunBasicVerification()
    {
        var code = @"using System;
using System.Collections.Generic;
using Fig.Contracts.SettingVerification;
namespace Fig.Api.SettingVerification.Unit.Test;
public class BasicVerification: ISettingVerification
{
    public VerificationResultDataContract PerformVerification(IDictionary<string, object?> settingValues)
    {
        return new VerificationResultDataContract()
        {
            Success = true,
            Message = ""The test was successful.""
        };
    }
}";
        var businessEntity = new SettingDynamicVerificationBusinessEntity
        {
            Code = code,
            Name = "BasicTest",
            Description = "This is a desc.",
            TargetRuntime = TargetRuntime.Dotnet6
        };

        var runner = new SettingDynamicVerifier(_codeHasherMock.Object);
        var result = await runner.RunVerification(businessEntity, new Dictionary<string, object?>());

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
    public VerificationResultDataContract PerformVerification(IDictionary<string, object?> settingValues)
    {
        throw new Exception(""No good"");
    }
}";
        var businessEntity = new SettingDynamicVerificationBusinessEntity
        {
            Code = code,
            Name = "BasicTest",
            Description = "This is a desc.",
            TargetRuntime = TargetRuntime.Dotnet6
        };

        var runner = new SettingDynamicVerifier(_codeHasherMock.Object);
        var result = await runner.RunVerification(businessEntity, new Dictionary<string, object?>());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message.StartsWith("Exception during code execution"));
        Assert.That(result.Message.Contains("No good"));
    }

    [Test]
    public async Task ShallHandleCompileErrorInProvidedCode()
    {
        var codeMissingUsingStatements = @"
namespace Fig.Api.SettingVerification.Unit.Test;
public class BasicVerification: ISettingVerification
{
    public VerificationResultDataContract PerformVerification(IDictionary<string, object?> settingValues)
    {
        return new VerificationResultDataContract();
    }
}";
        var businessEntity = new SettingDynamicVerificationBusinessEntity
        {
            Code = codeMissingUsingStatements,
            Name = "BasicTest",
            Description = "This is a desc.",
            TargetRuntime = TargetRuntime.Dotnet6
        };

        var runner = new SettingDynamicVerifier(_codeHasherMock.Object);
        var result = await runner.RunVerification(businessEntity, new Dictionary<string, object?>());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message.StartsWith("Compile error(s) detected in settings verification code"));
    }
}