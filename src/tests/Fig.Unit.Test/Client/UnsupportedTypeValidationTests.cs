using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Description;
using Fig.Client.DefaultValue;
using Fig.Client.Enums;
using Fig.Client.Exceptions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client
{
    [TestFixture]
    public class UnsupportedTypeValidationTests
    {
        private Mock<IDescriptionProvider> _descriptionProviderMock = null!;
        private Mock<IDataGridDefaultValueProvider> _dataGridDefaultValueProviderMock = null!;
        private SettingDefinitionFactory _factory = null!;

        [SetUp]
        public void Setup()
        {
            _descriptionProviderMock = new Mock<IDescriptionProvider>();
            _descriptionProviderMock.Setup(a => a.GetDescription(It.IsAny<string>())).Returns("Test description");
            _dataGridDefaultValueProviderMock = new Mock<IDataGridDefaultValueProvider>();
            
            _factory = new SettingDefinitionFactory(_descriptionProviderMock.Object, _dataGridDefaultValueProviderMock.Object);
        }

        [Test]
        public void CreateDataContract_WithFloatProperty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var settings = new TestSettingsWithFloat();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                settings.CreateDataContract("TestClient"));
            
            Assert.That(ex!.Message, Does.Contain("FloatSetting"));
            Assert.That(ex.Message, Does.Contain("not supported"));
            Assert.That(ex.Message, Does.Contain("float"));
        }

        [Test]
        public void CreateDataContract_WithDecimalProperty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var settings = new TestSettingsWithDecimal();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                settings.CreateDataContract("TestClient"));
            
            Assert.That(ex!.Message, Does.Contain("DecimalSetting"));
            Assert.That(ex.Message, Does.Contain("not supported"));
            Assert.That(ex.Message, Does.Contain("decimal"));
        }

        [Test]
        public void CreateDataContract_WithByteProperty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var settings = new TestSettingsWithByte();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                settings.CreateDataContract("TestClient"));
            
            Assert.That(ex!.Message, Does.Contain("ByteSetting"));
            Assert.That(ex.Message, Does.Contain("not supported"));
            Assert.That(ex.Message, Does.Contain("byte"));
        }

        [Test]
        public void CreateDataContract_WithMultipleUnsupportedTypes_ShouldThrowInvalidOperationExceptionWithAllErrors()
        {
            // Arrange
            var settings = new TestSettingsWithMultipleUnsupportedTypes();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                settings.CreateDataContract("TestClient"));
            
            // Check that all unsupported types are reported in a single formatted message
            Assert.That(ex!.Message, Does.Contain("2 issues found"));
            Assert.That(ex.Message, Does.Contain("FloatSetting"));
            Assert.That(ex.Message, Does.Contain("DecimalSetting"));
        }

        [Test]
        public void CreateDataContract_WithSupportedTypes_ShouldNotThrow()
        {
            // Arrange
            var settings = new TestSettingsWithSupportedTypes();

            // Act & Assert
            Assert.DoesNotThrow(() => settings.CreateDataContract("TestClient"));
        }

        [Test]
        public void CreateDataContract_WithCustomClass_ShouldNotThrow()
        {
            // Arrange
            var settings = new TestSettingsWithCustomClass();

            // Act & Assert
            Assert.DoesNotThrow(() => settings.CreateDataContract("TestClient"));
        }

        [Test]
        public void CreateDataContract_WithNullableUnsupportedType_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var settings = new TestSettingsWithNullableFloat();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                settings.CreateDataContract("TestClient"));
            
            Assert.That(ex!.Message, Does.Contain("NullableFloatSetting"));
            Assert.That(ex.Message, Does.Contain("not supported"));
        }
    }

    // Test classes

    public class TestSettingsWithFloat : SettingsBase
    {
        public override string ClientDescription => "Test client with float";

        [Setting("Float setting")]
        public float FloatSetting { get; set; } = 1.0f;

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }
    }

    public class TestSettingsWithDecimal : SettingsBase
    {
        public override string ClientDescription => "Test client with decimal";

        [Setting("Decimal setting")]
        public decimal DecimalSetting { get; set; } = 1.0m;

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }
    }

    public class TestSettingsWithByte : SettingsBase
    {
        public override string ClientDescription => "Test client with byte";

        [Setting("Byte setting")]
        public byte ByteSetting { get; set; } = 1;

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }
    }

    public class TestSettingsWithMultipleUnsupportedTypes : SettingsBase
    {
        public override string ClientDescription => "Test client with multiple unsupported types";

        [Setting("Float setting")]
        public float FloatSetting { get; set; } = 1.0f;

        [Setting("Decimal setting")]
        public decimal DecimalSetting { get; set; } = 1.0m;

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }
    }

    public class TestSettingsWithSupportedTypes : SettingsBase
    {
        public override string ClientDescription => "Test client with supported types";

        [Setting("Int setting")]
        public int IntSetting { get; set; } = 1;

        [Setting("Long setting")]
        public long LongSetting { get; set; } = 1L;

        [Setting("Double setting")]
        public double DoubleSetting { get; set; } = 1.0;

        [Setting("String setting")]
        public string StringSetting { get; set; } = "test";

        [Setting("Bool setting")]
        public bool BoolSetting { get; set; } = true;

        [Setting("DateTime setting")]
        public DateTime DateTimeSetting { get; set; } = DateTime.Now;

        [Setting("TimeSpan setting")]
        public TimeSpan TimeSpanSetting { get; set; } = TimeSpan.FromSeconds(1);

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }
    }

    public class TestSettingsWithCustomClass : SettingsBase
    {
        public override string ClientDescription => "Test client with custom class";

        [Setting("Custom class setting")]
        public CustomData CustomSetting { get; set; } = new CustomData();

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }

        public class CustomData
        {
            public string Name { get; set; } = "Test";
            public int Value { get; set; } = 42;
        }
    }

    public class TestSettingsWithNullableFloat : SettingsBase
    {
        public override string ClientDescription => "Test client with nullable float";

        [Setting("Nullable float setting")]
        public float? NullableFloatSetting { get; set; }

        public override IEnumerable<string> GetValidationErrors()
        {
            return Array.Empty<string>();
        }
    }
}
