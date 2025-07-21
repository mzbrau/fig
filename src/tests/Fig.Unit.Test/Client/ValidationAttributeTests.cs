using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Description;
using Fig.Client.DefaultValue;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client
{
    [TestFixture]
    public class ValidationAttributeTests
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
        public void PropertyLevelValidation_Takes_Precedence_Over_ClassLevelValidation()
        {
            // Arrange
            var settings = new PropertyAndClassLevelValidationSettings();
            var property = typeof(PropertyAndClassLevelValidationSettings).GetProperty(nameof(PropertyAndClassLevelValidationSettings.StringWithPropertyValidation));
            var settingDetails = new SettingDetails("TestSetting", property!, "stuff", "Test", "default");

            // Act
            var definition = _factory.Create(settingDetails, "Client1", 0, []);

            // Assert
            Assert.That(definition.ValidationRegex, Is.EqualTo(@"\d+")); // Should use property-level validation
            Assert.That(definition.ValidationExplanation, Is.EqualTo("Property validation"));
        }

        [Test]
        public void ClassLevelValidation_Is_Applied_To_Matching_Types_Without_PropertyValidation()
        {
            // Arrange
            var settings = new PropertyAndClassLevelValidationSettings();
            var property = typeof(PropertyAndClassLevelValidationSettings).GetProperty(nameof(PropertyAndClassLevelValidationSettings.StringWithoutValidation));
            var settingDetails = new SettingDetails("TestSetting", property!, "bla", "test", "default");

            // Act
            var definition = _factory.Create(settingDetails, "Client1", 0, []);

            // Assert
            Assert.That(definition.ValidationRegex, Is.EqualTo(@"[a-z]+")); // Should use class-level validation
            Assert.That(definition.ValidationExplanation, Is.EqualTo("Class validation"));
        }

        [Test]
        public void ClassLevelValidation_Is_Applied_To_Nullable_Types()
        {
            // Arrange
            var settings = new PropertyAndClassLevelValidationSettings();
            var property = typeof(PropertyAndClassLevelValidationSettings).GetProperty(nameof(PropertyAndClassLevelValidationSettings.NullableIntWithoutValidation));
            var settingDetails = new SettingDetails("TestSetting", property!, null, "test", 42);

            // Act
            var definition = _factory.Create(settingDetails, "Client1", 0, []);

            // Assert
            Assert.That(definition.ValidationRegex, Is.EqualTo(@"[1-9][0-9]*")); // Should use class-level validation for int
            Assert.That(definition.ValidationExplanation, Is.EqualTo("Positive numbers only"));
        }

        [Test]
        public void ClassLevelValidation_Is_Not_Applied_To_Unspecified_Types()
        {
            // Arrange
            var settings = new PropertyAndClassLevelValidationSettings();
            var property = typeof(PropertyAndClassLevelValidationSettings).GetProperty(nameof(PropertyAndClassLevelValidationSettings.BoolWithoutValidation));
            var settingDetails = new SettingDetails("TestSetting", property!, true, "test", true);

            // Act
            var definition = _factory.Create(settingDetails, "Client1", 0, []);

            // Assert
            Assert.That(definition.ValidationRegex, Is.Null); // No validation should be applied
            Assert.That(definition.ValidationExplanation, Is.Null);
        }
    }

    // Test classes

    [ValidationOfAllTypes("[a-z]+", "Class validation", true, typeof(string))]
    [ValidationOfAllTypes("[1-9][0-9]*", "Positive numbers only", true, typeof(int))]
    public class PropertyAndClassLevelValidationSettings
    {
        [Setting("test")] 
        [Validation(@"\d+", "Property validation")]
        public string StringWithPropertyValidation { get; set; } = "default";
        
        [Setting("test")]
        public string StringWithoutValidation { get; set; } = "default";
        
        [Setting("test")]
        public int? NullableIntWithoutValidation { get; set; }
        
        [Setting("test")]
        public bool BoolWithoutValidation { get; set; }
    }
}