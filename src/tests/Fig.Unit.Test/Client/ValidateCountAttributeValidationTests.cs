using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Description;
using Fig.Client.DefaultValue;
using Fig.Client.Exceptions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client
{
    [TestFixture]
    public class ValidateCountAttributeValidationTests
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
        public void ValidateCount_BetweenConstraint_WithSingleParameterConstructor_ShouldThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithBetweenConstraintWrongConstructor();
            var property = typeof(TestSettingsWithBetweenConstraintWrongConstructor).GetProperty(nameof(TestSettingsWithBetweenConstraintWrongConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert
            var ex = Assert.Throws<InvalidSettingException>(() => 
                _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
            
            Assert.That(ex!.Message, Does.Contain("[ValidateCount] on 'Items'"));
            Assert.That(ex.Message, Does.Contain("Between constraint requires the two-parameter constructor"));
            Assert.That(ex.Message, Does.Contain("[ValidateCount(Constraint.Between, lowerCount, higherCount)]"));
        }

        [Test]
        public void ValidateCount_ExactlyConstraint_WithTwoParameterConstructor_ShouldThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithExactlyConstraintWrongConstructor();
            var property = typeof(TestSettingsWithExactlyConstraintWrongConstructor).GetProperty(nameof(TestSettingsWithExactlyConstraintWrongConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert
            var ex = Assert.Throws<InvalidSettingException>(() => 
                _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
            
            Assert.That(ex!.Message, Does.Contain("[ValidateCount] on 'Items'"));
            Assert.That(ex.Message, Does.Contain("Exactly constraint requires the single-parameter constructor"));
            Assert.That(ex.Message, Does.Contain("[ValidateCount(Constraint.Exactly, count)]"));
        }

        [Test]
        public void ValidateCount_AtLeastConstraint_WithTwoParameterConstructor_ShouldThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithAtLeastConstraintWrongConstructor();
            var property = typeof(TestSettingsWithAtLeastConstraintWrongConstructor).GetProperty(nameof(TestSettingsWithAtLeastConstraintWrongConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert
            var ex = Assert.Throws<InvalidSettingException>(() => 
                _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
            
            Assert.That(ex!.Message, Does.Contain("[ValidateCount] on 'Items'"));
            Assert.That(ex.Message, Does.Contain("AtLeast constraint requires the single-parameter constructor"));
            Assert.That(ex.Message, Does.Contain("[ValidateCount(Constraint.AtLeast, count)]"));
        }

        [Test]
        public void ValidateCount_AtMostConstraint_WithTwoParameterConstructor_ShouldThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithAtMostConstraintWrongConstructor();
            var property = typeof(TestSettingsWithAtMostConstraintWrongConstructor).GetProperty(nameof(TestSettingsWithAtMostConstraintWrongConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert
            var ex = Assert.Throws<InvalidSettingException>(() => 
                _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
            
            Assert.That(ex!.Message, Does.Contain("[ValidateCount] on 'Items'"));
            Assert.That(ex.Message, Does.Contain("AtMost constraint requires the single-parameter constructor"));
            Assert.That(ex.Message, Does.Contain("[ValidateCount(Constraint.AtMost, count)]"));
        }

        [Test]
        public void ValidateCount_BetweenConstraint_WithValidConstructor_ShouldNotThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithBetweenConstraintCorrectConstructor();
            var property = typeof(TestSettingsWithBetweenConstraintCorrectConstructor).GetProperty(nameof(TestSettingsWithBetweenConstraintCorrectConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
        }

        [Test]
        public void ValidateCount_ExactlyConstraint_WithValidConstructor_ShouldNotThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithExactlyConstraintCorrectConstructor();
            var property = typeof(TestSettingsWithExactlyConstraintCorrectConstructor).GetProperty(nameof(TestSettingsWithExactlyConstraintCorrectConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
        }

        [Test]
        public void ValidateCount_AtLeastConstraint_WithValidConstructor_ShouldNotThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithAtLeastConstraintCorrectConstructor();
            var property = typeof(TestSettingsWithAtLeastConstraintCorrectConstructor).GetProperty(nameof(TestSettingsWithAtLeastConstraintCorrectConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
        }

        [Test]
        public void ValidateCount_AtMostConstraint_WithValidConstructor_ShouldNotThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithAtMostConstraintCorrectConstructor();
            var property = typeof(TestSettingsWithAtMostConstraintCorrectConstructor).GetProperty(nameof(TestSettingsWithAtMostConstraintCorrectConstructor.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
        }

        [Test]
        public void ValidateCount_BetweenConstraint_WithInvalidRange_ShouldThrowException()
        {
            // Arrange
            var settings = new TestSettingsWithBetweenConstraintInvalidRange();
            var property = typeof(TestSettingsWithBetweenConstraintInvalidRange).GetProperty(nameof(TestSettingsWithBetweenConstraintInvalidRange.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert
            var ex = Assert.Throws<InvalidSettingException>(() => 
                _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
            
            Assert.That(ex!.Message, Does.Contain("[ValidateCount] on 'Items'"));
            Assert.That(ex.Message, Does.Contain("Lower count (10) cannot be greater than higher count (5)"));
            Assert.That(ex.Message, Does.Contain("[ValidateCount(Constraint.Between, 1, 10)]"));
        }

        [Test]
        public void ValidateCount_ExactlyZero_WithValidConstructor_ShouldNotThrowException()
        {
            // Arrange - Test that using 0 as a valid count works (important edge case with explicit constructor tracking)
            var settings = new TestSettingsWithExactlyZeroConstraint();
            var property = typeof(TestSettingsWithExactlyZeroConstraint).GetProperty(nameof(TestSettingsWithExactlyZeroConstraint.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert - Should not throw even though count is 0
            Assert.DoesNotThrow(() => _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
        }

        [Test]
        public void ValidateCount_BetweenZeroToTwo_WithValidConstructor_ShouldNotThrowException()
        {
            // Arrange - Test that using 0 as lower bound in Between works (important edge case with explicit constructor tracking)
            var settings = new TestSettingsWithBetweenZeroToTwoConstraint();
            var property = typeof(TestSettingsWithBetweenZeroToTwoConstraint).GetProperty(nameof(TestSettingsWithBetweenZeroToTwoConstraint.Items));
            var settingDetails = new SettingDetails("", property!, null, "Items", settings);

            // Act & Assert - Should not throw even though lowerCount is 0
            Assert.DoesNotThrow(() => _factory.Create(settingDetails, "TestClient", 0, new List<SettingDetails> { settingDetails }));
        }
    }

    // Test classes

    public class TestSettingsWithBetweenConstraintWrongConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Between, 5)] // Wrong constructor - should use two-parameter constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithExactlyConstraintWrongConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Exactly, 2, 5)] // Wrong constructor - should use single-parameter constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithAtLeastConstraintWrongConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.AtLeast, 2, 5)] // Wrong constructor - should use single-parameter constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithAtMostConstraintWrongConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.AtMost, 2, 5)] // Wrong constructor - should use single-parameter constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithBetweenConstraintCorrectConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Between, 2, 5)] // Correct constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithExactlyConstraintCorrectConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Exactly, 5)] // Correct constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithAtLeastConstraintCorrectConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.AtLeast, 3)] // Correct constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithAtMostConstraintCorrectConstructor
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.AtMost, 10)] // Correct constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithBetweenConstraintInvalidRange
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Between, 10, 5)] // Invalid range - lower > higher
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithExactlyZeroConstraint
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Exactly, 0)] // Valid - using 0 as count with single-parameter constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }

    public class TestSettingsWithBetweenZeroToTwoConstraint
    {
        [Setting("Items", defaultValueMethodName: nameof(GetDefaultItems))]
        [ValidateCount(Constraint.Between, 0, 2)] // Valid - using 0 as lower bound with two-parameter constructor
        public List<string> Items { get; set; } = null!;

        public List<string> GetDefaultItems() => new();
    }
}
