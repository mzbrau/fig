﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.ExtensionMethods;

namespace Fig.Client.Integration.Test
{
    [TestFixture]
    public class TypeExtensionMethodsTest
    {

        [Test]
        [TestCase(typeof(bool), true)]
        [TestCase(typeof(char), true)]
        [TestCase(typeof(char), true)]
        [TestCase(typeof(double), true)]
        [TestCase(typeof(short), true)]
        [TestCase(typeof(int), true)]
        [TestCase(typeof(long), true)]
        [TestCase(typeof(float), true)]
        [TestCase(typeof(string), true)]
        [TestCase(typeof(List<string>), true)]
        [TestCase(typeof(Dictionary<string, string>), true)]
        [TestCase(typeof(string[]), true)]
        [TestCase(typeof(KeyValuePair<string, string>), true)]
        [TestCase(typeof(List<KeyValuePair<string, string>>), false)] // It would be nice to support this one.
        [TestCase(typeof(SomeClass), false)]
        [TestCase(typeof(Animals), true)]
        [TestCase(typeof(List<SomeClass>), false)]
        [TestCase(typeof(Dictionary<string, SomeClass>), false)]
        [TestCase(typeof(KeyValuePair<string, SomeClass>), false)]
        public void ShallReturnCorrectValueForSupportedTypes(Type type, bool isSupported)
        {
            var result = type.IsFigSupported();
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
}