using System;
using System.Linq;
using Fig.Client.Abstractions.StatusProperties;
using Fig.Client.StatusProperties;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Fig.Unit.Test.Client.StatusProperties;

[TestFixture]
public class FigStatusPropertiesTests
{
    private enum SampleMode
    {
        Idle,
        Active
    }

    private class SampleStatus
    {
        [StatusProperty(DisplayName = "Last Sync", Highlight = true, Order = 1)]
        public DateTime? LastSyncUtc { get; set; }

        [StatusProperty(DisplayName = "Queue", Highlight = true, Order = 2)]
        public long QueueDepth { get; set; }

        [StatusProperty(DisplayName = "Usage", Highlight = true, Order = 3)]
        public string Usage { get; set; } = "NORMAL";

        public TimeSpan? AverageLatency { get; set; }

        public SampleMode Mode { get; set; }

        public DateOnly? BusinessDate { get; set; }

        public TimeOnly? ShiftStart { get; set; }

        [StatusProperty(ShowInUi = false)]
        public string? SecretToken { get; set; }

        public NestedThing Nested { get; set; } = new();
    }

    private class NestedThing
    {
        public string Value { get; set; } = "x";
    }

    [Test]
    public void Set_ShouldUpdateSinglePropertyWithoutClearingOthers()
    {
        var store = new FigStatusProperties<SampleStatus>(NullLogger<FigStatusProperties<SampleStatus>>.Instance);
        store.Set(x => x.QueueDepth, 10);
        store.Set(x => x.LastSyncUtc, DateTime.UtcNow);
        store.Set(x => x.QueueDepth, 20);

        var current = store.Current;
        Assert.That(current.QueueDepth, Is.EqualTo(20));
        Assert.That(current.LastSyncUtc, Is.Not.Null);
    }

    [Test]
    public void Clear_ShouldResetPropertyToDefault()
    {
        var store = new FigStatusProperties<SampleStatus>();
        store.Set(x => x.QueueDepth, 5);
        store.Clear(x => x.QueueDepth);
        Assert.That(store.Current.QueueDepth, Is.EqualTo(0));
    }

    [Test]
    public void CreateSnapshot_ShouldIncludeFlagsAndSkipUnsupportedTypes()
    {
        var store = new FigStatusProperties<SampleStatus>(NullLogger<FigStatusProperties<SampleStatus>>.Instance);
        store.Update(x =>
        {
            x.LastSyncUtc = DateTime.UtcNow;
            x.QueueDepth = 3;
            x.AverageLatency = TimeSpan.FromMilliseconds(250);
            x.Mode = SampleMode.Active;
            x.SecretToken = "hidden";
        });

        var snapshot = store.CreateSnapshot();
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Properties.Any(p => p.Name == nameof(SampleStatus.Nested)), Is.False);

        var lastSync = snapshot.Properties.Single(p => p.Name == nameof(SampleStatus.LastSyncUtc));
        Assert.That(lastSync.Highlight, Is.True);
        Assert.That(lastSync.DisplayName, Is.EqualTo("Last Sync"));
        Assert.That(lastSync.Order, Is.EqualTo(1));

        var secret = snapshot.Properties.Single(p => p.Name == nameof(SampleStatus.SecretToken));
        Assert.That(secret.ShowInUi, Is.False);

        var mode = snapshot.Properties.Single(p => p.Name == nameof(SampleStatus.Mode));
        Assert.That(mode.ValueType, Is.EqualTo(CustomStatusValueType.Enum));
        Assert.That(mode.Value, Is.EqualTo(nameof(SampleMode.Active)));

        var latency = snapshot.Properties.Single(p => p.Name == nameof(SampleStatus.AverageLatency));
        Assert.That(latency.ValueType, Is.EqualTo(CustomStatusValueType.TimeSpan));
        Assert.That(latency.Highlight, Is.False);

        Assert.That(CustomStatusPropertiesValidator.TryValidate(snapshot, out var error), Is.True, error);
    }

    [Test]
    public void CreateSnapshot_ShouldSerializeDateOnlyAndTimeOnlyInInvariantWireFormats()
    {
        var store = new FigStatusProperties<SampleStatus>();
        store.Update(x =>
        {
            x.BusinessDate = new DateOnly(2026, 8, 10);
            x.ShiftStart = new TimeOnly(9, 30, 15);
        });

        var snapshot = store.CreateSnapshot();
        Assert.That(snapshot, Is.Not.Null);

        var businessDate = snapshot!.Properties.Single(p => p.Name == nameof(SampleStatus.BusinessDate));
        Assert.That(businessDate.ValueType, Is.EqualTo(CustomStatusValueType.DateOnly));
        Assert.That(businessDate.Value, Is.EqualTo("2026-08-10"));

        var shiftStart = snapshot.Properties.Single(p => p.Name == nameof(SampleStatus.ShiftStart));
        Assert.That(shiftStart.ValueType, Is.EqualTo(CustomStatusValueType.TimeOnly));
        Assert.That(shiftStart.Value, Is.EqualTo("09:30:15.0000000"));

        Assert.That(CustomStatusPropertiesValidator.TryValidate(snapshot, out var error), Is.True, error);
    }

    [Test]
    public void Set_WithTextColor_ShouldRoundTripIntoSnapshot()
    {
        var store = new FigStatusProperties<SampleStatus>();
        store.Set(x => x.Usage, "HIGH", "#E53935");

        var snapshot = store.CreateSnapshot();
        var usage = snapshot!.Properties.Single(p => p.Name == nameof(SampleStatus.Usage));
        Assert.That(usage.Value, Is.EqualTo("HIGH"));
        Assert.That(usage.TextColor, Is.EqualTo("#E53935"));
        Assert.That(CustomStatusPropertiesValidator.TryValidate(snapshot, out var error), Is.True, error);
    }

    [Test]
    public void Set_WithoutTextColor_ShouldLeavePreviousColorUnchanged()
    {
        var store = new FigStatusProperties<SampleStatus>();
        store.Set(x => x.Usage, "HIGH", "#E53935");
        store.Set(x => x.Usage, "LOW");

        var usage = store.CreateSnapshot()!.Properties.Single(p => p.Name == nameof(SampleStatus.Usage));
        Assert.That(usage.Value, Is.EqualTo("LOW"));
        Assert.That(usage.TextColor, Is.EqualTo("#E53935"));
    }

    [Test]
    public void Clear_ShouldRemoveTextColor()
    {
        var store = new FigStatusProperties<SampleStatus>();
        store.Set(x => x.Usage, "HIGH", "#E53935");
        store.Clear(x => x.Usage);

        var usage = store.CreateSnapshot()!.Properties.Single(p => p.Name == nameof(SampleStatus.Usage));
        Assert.That(usage.Value, Is.EqualTo(string.Empty).Or.Null);
        Assert.That(usage.TextColor, Is.Null);
    }

    [Test]
    public void SetTextColor_Null_ShouldClearColorWithoutChangingValue()
    {
        var store = new FigStatusProperties<SampleStatus>();
        store.Set(x => x.Usage, "HIGH", "#E53935");
        store.SetTextColor(x => x.Usage, null);

        var usage = store.CreateSnapshot()!.Properties.Single(p => p.Name == nameof(SampleStatus.Usage));
        Assert.That(usage.Value, Is.EqualTo("HIGH"));
        Assert.That(usage.TextColor, Is.Null);
    }

    [Test]
    public void Set_WithInvalidTextColor_ShouldThrow()
    {
        var store = new FigStatusProperties<SampleStatus>();
        Assert.That(() => store.Set(x => x.Usage, "HIGH", "red"),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("textColor"));
    }

    [Test]
    public void CreateSnapshot_ShouldSkipUintThatOverflowsInt()
    {
        var store = new FigStatusProperties<UintStatus>(NullLogger<FigStatusProperties<UintStatus>>.Instance);
        store.Set(x => x.SafeCount, 42u);
        store.Set(x => x.HugeCount, uint.MaxValue);

        var snapshot = store.CreateSnapshot();
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Properties.Any(p => p.Name == nameof(UintStatus.HugeCount)), Is.False);
        Assert.That(snapshot.Properties.Single(p => p.Name == nameof(UintStatus.SafeCount)).Value, Is.EqualTo(42));
    }

    private class UintStatus
    {
        public uint SafeCount { get; set; }

        public uint HugeCount { get; set; }
    }
}

[TestFixture]
public class CustomStatusPropertiesValidatorTests
{
    [Test]
    public void Validate_ShouldRejectTooManyProperties()
    {
        var properties = new CustomStatusPropertiesDataContract(
            Enumerable.Range(0, CustomStatusPropertiesLimits.MaxProperties + 1)
                .Select(i => new CustomStatusPropertyDataContract($"P{i}", CustomStatusValueType.Integer, i))
                .ToList());

        Assert.That(() => CustomStatusPropertiesValidator.ValidateOrThrow(properties),
            Throws.ArgumentException);
    }

    [Test]
    public void Validate_ShouldRejectOversizedString()
    {
        var properties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("Huge", CustomStatusValueType.String,
                new string('a', CustomStatusPropertiesLimits.MaxStringValueLength + 1))
        ]);

        Assert.That(CustomStatusPropertiesValidator.TryValidate(properties, out var error), Is.False);
        Assert.That(error, Does.Contain("exceeds"));
    }

    [Test]
    public void Validate_ShouldRejectInvalidTextColor()
    {
        var properties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("Usage", CustomStatusValueType.String, "HIGH",
                textColor: "red")
        ]);

        Assert.That(CustomStatusPropertiesValidator.TryValidate(properties, out var error), Is.False);
        Assert.That(error, Does.Contain("TextColor"));
    }

    [Test]
    public void Validate_ShouldAcceptHexTextColors()
    {
        var properties = new CustomStatusPropertiesDataContract(
        [
            new CustomStatusPropertyDataContract("A", CustomStatusValueType.String, "x", textColor: "#f00"),
            new CustomStatusPropertyDataContract("B", CustomStatusValueType.String, "y", textColor: "#E53935")
        ]);

        Assert.That(CustomStatusPropertiesValidator.TryValidate(properties, out var error), Is.True, error);
    }
}
