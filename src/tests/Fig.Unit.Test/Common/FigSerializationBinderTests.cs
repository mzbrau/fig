using System;
using Fig.Common.NetStandard.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Common;

[TestFixture]
public class FigSerializationBinderTests
{
    private FigSerializationBinder _binder = null!;

    [SetUp]
    public void Setup()
    {
        _binder = new FigSerializationBinder();
    }

    [Test]
    public void ShallAllowFigContractsAssembly()
    {
        // StringSettingDataContract actually exists in Fig.Contracts
        Assert.DoesNotThrow(() => _binder.BindToType(
            "Fig.Contracts", "Fig.Contracts.Settings.StringSettingDataContract"));
    }

    [Test]
    public void ShallAllowFigContractsWithVersionInfo()
    {
        Assert.DoesNotThrow(() => _binder.BindToType(
            "Fig.Contracts, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Fig.Contracts.Settings.StringSettingDataContract"));
    }

    [TestCase("Fig.Api")]
    [TestCase("Fig.Integration")]
    [TestCase("Fig.Test")]
    [TestCase("Fig.Api, Version=1.0.0.0")]
    public void ShallRejectTypesFromServerAssemblies(string assembly)
    {
        Assert.Throws<InvalidOperationException>(() =>
            _binder.BindToType(assembly, "Some.Type"));
    }

    [TestCase("System.String")]
    [TestCase("System.Int32")]
    [TestCase("System.Boolean")]
    [TestCase("System.DateTime")]
    [TestCase("System.Guid")]
    [TestCase("System.Decimal")]
    public void ShallAllowSystemPrimitives(string typeName)
    {
        // Primitives resolve with null assembly
        Assert.DoesNotThrow(() => _binder.BindToType(null, typeName));
    }

    [TestCase("System.IO.FileInfo")]
    [TestCase("System.Diagnostics.Process")]
    public void ShallRejectArbitrarySystemTypes(string typeName)
    {
        Assert.Throws<InvalidOperationException>(() =>
            _binder.BindToType(null, typeName));
    }

    [Test]
    public void ShallRejectTypesWithFigPrefixFromUnknownAssembly()
    {
        // The Fig. prefix bypass was removed — assembly must be in allowlist
        Assert.Throws<InvalidOperationException>(() =>
            _binder.BindToType("Evil.Assembly", "Fig.Evil.Malicious"));
    }

    [Test]
    public void ShallAllowListOfAllowedType()
    {
        Assert.DoesNotThrow(() => _binder.BindToType(null,
            "System.Collections.Generic.List`1[[System.String, mscorlib]]"));
    }

    [Test]
    public void ShallAllowDictionaryOfAllowedTypes()
    {
        Assert.DoesNotThrow(() => _binder.BindToType(null,
            "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]]"));
    }

    [Test]
    public void ShallAllowListOfFigContractType()
    {
        Assert.DoesNotThrow(() => _binder.BindToType(null,
            "System.Collections.Generic.List`1[[Fig.Contracts.Settings.StringSettingDataContract, Fig.Contracts]]"));
    }

    [Test]
    public void ShallRejectListOfDisallowedType()
    {
        Assert.Throws<InvalidOperationException>(() => _binder.BindToType(null,
            "System.Collections.Generic.List`1[[System.IO.FileInfo, System.IO.FileSystem]]"));
    }

    [Test]
    public void ShallRejectDictionaryWithDisallowedValueType()
    {
        Assert.Throws<InvalidOperationException>(() => _binder.BindToType(null,
            "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.IO.FileInfo, System.IO.FileSystem]]"));
    }

    [Test]
    public void ShallRejectGenericWithServerAssemblyType()
    {
        Assert.Throws<InvalidOperationException>(() => _binder.BindToType(null,
            "System.Collections.Generic.List`1[[Fig.Api.SomeType, Fig.Api]]"));
    }

    [Test]
    public void ShallAllowNestedGenericWithAllowedTypes()
    {
        Assert.DoesNotThrow(() => _binder.BindToType(null,
            "System.Collections.Generic.List`1[[System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]], mscorlib]]"));
    }

    [Test]
    public void ShallRejectSingleBracketGenericBypass()
    {
        // Single-bracket format should be rejected (bypass attempt)
        Assert.Throws<InvalidOperationException>(() => _binder.BindToType(null,
            "System.Collections.Generic.List`1[System.IO.FileInfo]"));
    }
}
