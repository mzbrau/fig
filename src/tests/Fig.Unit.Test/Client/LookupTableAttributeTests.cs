using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Description;
using Fig.Client.DefaultValue;
using Fig.Client.Enums;
using Fig.Client.Exceptions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client
{
    [TestFixture]
    public class LookupTableAttributeTests
    {
        private Mock<IDescriptionProvider> _descriptionProviderMock = null!;
        private Mock<IDataGridDefaultValueProvider> _dataGridDefaultValueProviderMock = null!;
        private SettingDefinitionFactory _factory = null!;

        [SetUp]
        public void Setup()
        {
            _descriptionProviderMock = new Mock<IDescriptionProvider>();
            _descriptionProviderMock.Setup(a => a.GetDescription(It.IsAny<string>())).Returns("Desc");
            _dataGridDefaultValueProviderMock = new Mock<IDataGridDefaultValueProvider>();
            
            _factory = new SettingDefinitionFactory(_descriptionProviderMock.Object, _dataGridDefaultValueProviderMock.Object);
        }

        [Test]
        public void LookupTable_WithValidKeySettingName_ShouldNotThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithValidLookupTable();
            var keySettingProperty = typeof(TestSettingsWithValidLookupTable).GetProperty(nameof(TestSettingsWithValidLookupTable.KeySetting));
            var lookupSettingProperty = typeof(TestSettingsWithValidLookupTable).GetProperty(nameof(TestSettingsWithValidLookupTable.LookupSetting));
            
            var keySettingDetails = new SettingDetails("", keySettingProperty!, "default", "KeySetting", settings);
            var lookupSettingDetails = new SettingDetails("", lookupSettingProperty!, "default", "LookupSetting", settings);
            
            var allSettings = new List<SettingDetails> { keySettingDetails, lookupSettingDetails };

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _factory.Create(lookupSettingDetails, "TestClient", 2, allSettings));
        }

        [Test]
        public void LookupTable_WithInvalidKeySettingName_ShouldThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithInvalidLookupTable();
            var keySettingProperty = typeof(TestSettingsWithInvalidLookupTable).GetProperty(nameof(TestSettingsWithInvalidLookupTable.KeySetting));
            var lookupSettingProperty = typeof(TestSettingsWithInvalidLookupTable).GetProperty(nameof(TestSettingsWithInvalidLookupTable.LookupSetting));
            
            var keySettingDetails = new SettingDetails("", keySettingProperty!, "default", "KeySetting", settings);
            var lookupSettingDetails = new SettingDetails("", lookupSettingProperty!, "default", "LookupSetting", settings);
            
            var allSettings = new List<SettingDetails> { keySettingDetails, lookupSettingDetails };

            // Act & Assert
            var ex = Assert.Throws<InvalidSettingException>(() => 
                _factory.Create(lookupSettingDetails, "TestClient", 2, allSettings));
            
            Assert.That(ex!.Message, Does.Contain("KeySettingName 'NonExistentSetting' which does not match any setting name"));
            Assert.That(ex.Message, Does.Contain("Available setting names: KeySetting, LookupSetting"));
        }

        [Test]
        public void LookupTable_WithNullKeySettingName_ShouldNotValidate()
        {
            // Arrange
            var settings = new TestSettingsWithNullKeySettingName();
            var keySettingProperty = typeof(TestSettingsWithNullKeySettingName).GetProperty(nameof(TestSettingsWithNullKeySettingName.KeySetting));
            var lookupSettingProperty = typeof(TestSettingsWithNullKeySettingName).GetProperty(nameof(TestSettingsWithNullKeySettingName.LookupSetting));
            
            var keySettingDetails = new SettingDetails("", keySettingProperty!, "default", "KeySetting", settings);
            var lookupSettingDetails = new SettingDetails("", lookupSettingProperty!, "default", "LookupSetting", settings);
            
            var allSettings = new List<SettingDetails> { keySettingDetails, lookupSettingDetails };

            // Act & Assert - Should not throw since KeySettingName is null
            Assert.DoesNotThrow(() => _factory.Create(lookupSettingDetails, "TestClient", 2, allSettings));
        }

        [Test]
        public void LookupTable_WithEmptyKeySettingName_ShouldNotValidate()
        {
            // Arrange
            var settings = new TestSettingsWithEmptyKeySettingName();
            var keySettingProperty = typeof(TestSettingsWithEmptyKeySettingName).GetProperty(nameof(TestSettingsWithEmptyKeySettingName.KeySetting));
            var lookupSettingProperty = typeof(TestSettingsWithEmptyKeySettingName).GetProperty(nameof(TestSettingsWithEmptyKeySettingName.LookupSetting));
            
            var keySettingDetails = new SettingDetails("", keySettingProperty!, "default", "KeySetting", settings);
            var lookupSettingDetails = new SettingDetails("", lookupSettingProperty!, "default", "LookupSetting", settings);
            
            var allSettings = new List<SettingDetails> { keySettingDetails, lookupSettingDetails };

            // Act & Assert - Should not throw since KeySettingName is empty
            Assert.DoesNotThrow(() => _factory.Create(lookupSettingDetails, "TestClient", 2, allSettings));
        }

        [Test]
        public void LookupTable_WithoutAllSettingsParameter_ShouldNotValidate()
        {
            // Arrange
            var settings = new TestSettingsWithInvalidLookupTable();
            var lookupSettingProperty = typeof(TestSettingsWithInvalidLookupTable).GetProperty(nameof(TestSettingsWithInvalidLookupTable.LookupSetting));
            var lookupSettingDetails = new SettingDetails("", lookupSettingProperty!, "default", "LookupSetting", settings);

            // Act & Assert - Should not throw since allSettings parameter is null (backward compatibility)
            Assert.DoesNotThrow(() => _factory.Create(lookupSettingDetails, "TestClient", 2));
        }
    }

    // Test classes

    public class TestSettingsWithValidLookupTable
    {
        [Setting("Key Setting")]
        public string KeySetting { get; set; } = "default";

        [Setting("Lookup Setting")]
        [LookupTable("MyTable", LookupSource.UserDefined, "KeySetting")]
        public string LookupSetting { get; set; } = "default";
    }

    public class TestSettingsWithInvalidLookupTable
    {
        [Setting("Key Setting")]
        public string KeySetting { get; set; } = "default";

        [Setting("Lookup Setting")]
        [LookupTable("MyTable", LookupSource.UserDefined, "NonExistentSetting")]
        public string LookupSetting { get; set; } = "default";
    }

    public class TestSettingsWithNullKeySettingName
    {
        [Setting("Key Setting")]
        public string KeySetting { get; set; } = "default";

        [Setting("Lookup Setting")]
        [LookupTable("MyTable", LookupSource.UserDefined, null)]
        public string LookupSetting { get; set; } = "default";
    }

    public class TestSettingsWithEmptyKeySettingName
    {
        [Setting("Key Setting")]
        public string KeySetting { get; set; } = "default";

        [Setting("Lookup Setting")]
        [LookupTable("MyTable", LookupSource.UserDefined, "")]
        public string LookupSetting { get; set; } = "default";
    }
}
