using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Description;
using Fig.Client.DefaultValue;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client
{
    [TestFixture]
    public class ApplyAutomaticHeadingGenerationTests
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
        public void ApplyAutomaticHeadingGeneration_WhenDisabled_ShouldNotAddHeading()
        {
            // Arrange
            var settings = new TestSettingsWithCategory();
            var property = typeof(TestSettingsWithCategory).GetProperty(nameof(TestSettingsWithCategory.FirstSetting));
            var settingDetails = new SettingDetails("", property!, "default", "FirstSetting", settings);
            var allSettings = new List<SettingDetails> { settingDetails };

            // Act
            var result = _factory.Create(settingDetails, "TestClient", 0, allSettings, automaticallyGenerateHeadings: false);

            // Assert
            Assert.That(result.Heading, Is.Null);
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_WhenManualHeadingExists_ShouldNotOverride()
        {
            // Arrange
            var settings = new TestSettingsWithManualHeading();
            var property = typeof(TestSettingsWithManualHeading).GetProperty(nameof(TestSettingsWithManualHeading.SettingWithManualHeading));
            var settingDetails = new SettingDetails("", property!, "default", "SettingWithManualHeading", settings);
            var allSettings = new List<SettingDetails> { settingDetails };

            // Act
            var result = _factory.Create(settingDetails, "TestClient", 0, allSettings, automaticallyGenerateHeadings: true);

            // Assert
            Assert.That(result.Heading, Is.Not.Null);
            Assert.That(result.Heading!.Text, Is.EqualTo("Manual Heading"));
            Assert.That(result.Heading.Color, Is.EqualTo("#FF0000"));
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_WhenNoCategoryName_ShouldNotAddHeading()
        {
            // Arrange
            var settings = new TestSettingsWithoutCategory();
            var property = typeof(TestSettingsWithoutCategory).GetProperty(nameof(TestSettingsWithoutCategory.SettingWithoutCategory));
            var settingDetails = new SettingDetails("", property!, "default", "SettingWithoutCategory", settings);
            var allSettings = new List<SettingDetails> { settingDetails };

            // Act
            var result = _factory.Create(settingDetails, "TestClient", 0, allSettings, automaticallyGenerateHeadings: true);

            // Assert
            Assert.That(result.Heading, Is.Null);
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_ForFirstSettingWithCategory_ShouldAddHeading()
        {
            // Arrange
            var settings = new TestSettingsWithCategory();
            var firstProperty = typeof(TestSettingsWithCategory).GetProperty(nameof(TestSettingsWithCategory.FirstSetting));
            var firstSettingDetails = new SettingDetails("", firstProperty!, "default", "FirstSetting", settings);
            var allSettings = new List<SettingDetails> { firstSettingDetails };

            // Act
            var result = _factory.Create(firstSettingDetails, "TestClient", 0, allSettings, automaticallyGenerateHeadings: true);

            // Assert
            Assert.That(result.Heading, Is.Not.Null);
            Assert.That(result.Heading!.Text, Is.EqualTo("Database"));
            Assert.That(result.Heading.Color, Is.EqualTo("#0066CC"));
            Assert.That(result.Heading.Advanced, Is.False);
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_ForSecondSettingWithSameCategory_ShouldNotAddHeading()
        {
            // Arrange
            var settings = new TestSettingsWithCategory();
            var firstProperty = typeof(TestSettingsWithCategory).GetProperty(nameof(TestSettingsWithCategory.FirstSetting));
            var secondProperty = typeof(TestSettingsWithCategory).GetProperty(nameof(TestSettingsWithCategory.SecondSetting));
            
            var firstSettingDetails = new SettingDetails("", firstProperty!, "default", "FirstSetting", settings);
            var secondSettingDetails = new SettingDetails("", secondProperty!, "default", "SecondSetting", settings);
            var allSettings = new List<SettingDetails> { firstSettingDetails, secondSettingDetails };

            // Act
            var result = _factory.Create(secondSettingDetails, "TestClient", 1, allSettings, automaticallyGenerateHeadings: true);

            // Assert
            Assert.That(result.Heading, Is.Null);
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_ForFirstSettingWithNewCategory_ShouldAddHeading()
        {
            // Arrange
            var settings = new TestSettingsWithMultipleCategories();
            var databaseProperty = typeof(TestSettingsWithMultipleCategories).GetProperty(nameof(TestSettingsWithMultipleCategories.DatabaseSetting));
            var loggingProperty = typeof(TestSettingsWithMultipleCategories).GetProperty(nameof(TestSettingsWithMultipleCategories.LoggingSetting));
            
            var databaseSettingDetails = new SettingDetails("", databaseProperty!, "default", "DatabaseSetting", settings);
            var loggingSettingDetails = new SettingDetails("", loggingProperty!, "default", "LoggingSetting", settings);
            var allSettings = new List<SettingDetails> { databaseSettingDetails, loggingSettingDetails };

            // Act - Create the second setting (first with Logging category)
            var result = _factory.Create(loggingSettingDetails, "TestClient", 1, allSettings, automaticallyGenerateHeadings: true);

            // Assert
            Assert.That(result.Heading, Is.Not.Null);
            Assert.That(result.Heading!.Text, Is.EqualTo("Logging"));
            Assert.That(result.Heading.Color, Is.EqualTo("#FF6600"));
            Assert.That(result.Heading.Advanced, Is.False);
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_WithAdvancedSetting_ShouldInheritAdvancedFlag()
        {
            // Arrange
            var settings = new TestSettingsWithAdvanced();
            var property = typeof(TestSettingsWithAdvanced).GetProperty(nameof(TestSettingsWithAdvanced.AdvancedSetting));
            var settingDetails = new SettingDetails("", property!, "default", "AdvancedSetting", settings);
            var allSettings = new List<SettingDetails> { settingDetails };

            // Act
            var result = _factory.Create(settingDetails, "TestClient", 0, allSettings, automaticallyGenerateHeadings: true);

            // Assert
            Assert.That(result.Heading, Is.Not.Null);
            Assert.That(result.Heading!.Text, Is.EqualTo("Advanced"));
            Assert.That(result.Heading.Color, Is.EqualTo("#999999"));
            Assert.That(result.Heading.Advanced, Is.True);
        }

        [Test]
        public void ApplyAutomaticHeadingGeneration_WithMultipleCategoriesInterleaved_ShouldAddHeadingOnlyForFirst()
        {
            // Arrange
            var settings = new TestSettingsWithInterleavedCategories();
            var db1Property = typeof(TestSettingsWithInterleavedCategories).GetProperty(nameof(TestSettingsWithInterleavedCategories.Database1));
            var log1Property = typeof(TestSettingsWithInterleavedCategories).GetProperty(nameof(TestSettingsWithInterleavedCategories.Logging1));
            var db2Property = typeof(TestSettingsWithInterleavedCategories).GetProperty(nameof(TestSettingsWithInterleavedCategories.Database2));
            var log2Property = typeof(TestSettingsWithInterleavedCategories).GetProperty(nameof(TestSettingsWithInterleavedCategories.Logging2));
            
            var db1Details = new SettingDetails("", db1Property!, "default", "Database1", settings);
            var log1Details = new SettingDetails("", log1Property!, "default", "Logging1", settings);
            var db2Details = new SettingDetails("", db2Property!, "default", "Database2", settings);
            var log2Details = new SettingDetails("", log2Property!, "default", "Logging2", settings);
            var allSettings = new List<SettingDetails> { db1Details, log1Details, db2Details, log2Details };

            // Act & Assert
            var db1Result = _factory.Create(db1Details, "TestClient", 0, allSettings, automaticallyGenerateHeadings: true);
            Assert.That(db1Result.Heading, Is.Not.Null);
            Assert.That(db1Result.Heading!.Text, Is.EqualTo("Database"));

            var log1Result = _factory.Create(log1Details, "TestClient", 1, allSettings, automaticallyGenerateHeadings: true);
            Assert.That(log1Result.Heading, Is.Not.Null);
            Assert.That(log1Result.Heading!.Text, Is.EqualTo("Logging"));

            var db2Result = _factory.Create(db2Details, "TestClient", 2, allSettings, automaticallyGenerateHeadings: true);
            Assert.That(db2Result.Heading, Is.Null); // Should not add heading as Database category already seen

            var log2Result = _factory.Create(log2Details, "TestClient", 3, allSettings, automaticallyGenerateHeadings: true);
            Assert.That(log2Result.Heading, Is.Null); // Should not add heading as Logging category already seen
        }
    }

    // Test classes

    public class TestSettingsWithCategory
    {
        [Setting("First database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string FirstSetting { get; set; } = "default";

        [Setting("Second database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string SecondSetting { get; set; } = "default";
    }

    public class TestSettingsWithManualHeading
    {
        [Setting("Setting with manual heading")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Heading("Manual Heading", "#FF0000")]
        public string SettingWithManualHeading { get; set; } = "default";
    }

    public class TestSettingsWithoutCategory
    {
        [Setting("Setting without category")]
        public string SettingWithoutCategory { get; set; } = "default";
    }

    public class TestSettingsWithMultipleCategories
    {
        [Setting("Database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string DatabaseSetting { get; set; } = "default";

        [Setting("Logging setting")]
        [Fig.Client.Abstractions.Attributes.Category("Logging", "#FF6600")]
        public string LoggingSetting { get; set; } = "default";
    }

    public class TestSettingsWithAdvanced
    {
        [Setting("Advanced setting")]
        [Fig.Client.Abstractions.Attributes.Category("Advanced", "#999999")]
        [Advanced]
        public string AdvancedSetting { get; set; } = "default";
    }

    public class TestSettingsWithInterleavedCategories
    {
        [Setting("First database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string Database1 { get; set; } = "default";

        [Setting("First logging setting")]
        [Fig.Client.Abstractions.Attributes.Category("Logging", "#FF6600")]
        public string Logging1 { get; set; } = "default";

        [Setting("Second database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string Database2 { get; set; } = "default";

        [Setting("Second logging setting")]
        [Fig.Client.Abstractions.Attributes.Category("Logging", "#FF6600")]
        public string Logging2 { get; set; } = "default";
    }
}
