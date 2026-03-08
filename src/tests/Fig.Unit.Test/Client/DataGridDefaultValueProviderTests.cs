using System.Collections.Generic;
using Fig.Client.DefaultValue;
using Fig.Contracts.SettingDefinitions;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

public class DataGridDefaultValueProviderTests
{
    private DataGridDefaultValueProvider _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new DataGridDefaultValueProvider();
    }

    [Test]
    public void ShallConvertSimpleSingleColumnList()
    {
        var value = new List<string> { "one", "two" };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Values", typeof(string))
        };

        var result = _sut.Convert(value, columns);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result[0]["Values"], Is.EqualTo("one"));
        Assert.That(result[1]["Values"], Is.EqualTo("two"));
    }

    [Test]
    public void ShallConvertComplexSingleColumnList()
    {
        var value = new List<SinglePropertyClass>
        {
            new() { Name = "first" },
            new() { Name = "second" }
        };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string))
        };

        var result = _sut.Convert(value, columns);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        // Should extract the property value, not the whole object
        Assert.That(result[0]["Name"], Is.EqualTo("first"));
        Assert.That(result[1]["Name"], Is.EqualTo("second"));
    }

    [Test]
    public void ShallConvertComplexMultiColumnList()
    {
        var value = new List<MultiPropertyClass>
        {
            new() { Name = "first", Value = 42 }
        };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Value", typeof(int))
        };

        var result = _sut.Convert(value, columns);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0]["Name"], Is.EqualTo("first"));
        Assert.That(result[0]["Value"], Is.EqualTo(42));
    }

    public class SinglePropertyClass
    {
        public string Name { get; set; } = string.Empty;
    }

    public class MultiPropertyClass
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
