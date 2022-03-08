using System;
using System.Collections.Generic;
using Fig.Client.ExtensionMethods;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

[TestFixture]
public class TypeExtensionMethodsTest
{
    [Test]
    [TestCase(typeof(bool), true)]
    [TestCase(typeof(char), false)]
    [TestCase(typeof(double), true)]
    [TestCase(typeof(short), false)]
    [TestCase(typeof(int), true)]
    [TestCase(typeof(long), true)]
    [TestCase(typeof(float), false)]
    [TestCase(typeof(string), true)]
    [TestCase(typeof(List<string>), false)]
    [TestCase(typeof(Dictionary<string, string>), false)]
    [TestCase(typeof(string[]), false)]
    [TestCase(typeof(KeyValuePair<string, string>), false)]
    [TestCase(typeof(List<KeyValuePair<string, string>>), false)]
    [TestCase(typeof(SomeClass), false)]
    [TestCase(typeof(Animals), true)]
    [TestCase(typeof(List<SomeClass>), false)]
    [TestCase(typeof(Dictionary<string, SomeClass>), false)]
    [TestCase(typeof(KeyValuePair<string, SomeClass>), false)]
    [TestCase(typeof(DateTime), true)]
    [TestCase(typeof(DateOnly), false)]
    [TestCase(typeof(TimeOnly), false)]
    [TestCase(typeof(TimeSpan), true)]
    public void ShallReturnCorrectValueForSupportedTypes(Type type, bool isSupported)
    {
        var result = type.IsSupportedBaseType();
        Assert.That(result, Is.EqualTo(isSupported), $"{type.Name} -> IsSupported:{isSupported}");
    }

    [TestCase(typeof(string), false)]
    [TestCase(typeof(List<string>), true)]
    [TestCase(typeof(Dictionary<string, string>), false)]
    [TestCase(typeof(string[]), false)]
    [TestCase(typeof(KeyValuePair<string, string>), false)]
    [TestCase(typeof(List<KeyValuePair<string, string>>), false)]
    [TestCase(typeof(SomeClass), false)]
    [TestCase(typeof(Animals), false)]
    [TestCase(typeof(List<SomeClass>), true)]
    [TestCase(typeof(Dictionary<string, SomeClass>), false)]
    [TestCase(typeof(KeyValuePair<string, SomeClass>), false)]
    public void ShallSupportDataGridTypes(Type type, bool isSupported)
    {
        var result = type.IsSupportedDataGridType();
        Assert.That(result, Is.EqualTo(isSupported), $"{type.Name} -> IsSupported:{isSupported}");
    }

    public class SomeClass
    {
        public string? Sample { get; set; }
    }

    public enum Animals
    {
        Cat,
        Dog
    }
}