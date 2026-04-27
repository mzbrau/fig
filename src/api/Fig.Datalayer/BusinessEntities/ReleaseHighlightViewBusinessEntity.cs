namespace Fig.Datalayer.BusinessEntities;

public class ReleaseHighlightViewBusinessEntity
{
    public virtual Guid Id { get; init; }

    public virtual Guid UserId { get; set; }

    public virtual string ReleaseVersion { get; set; } = default!;

    public virtual string FeatureKey { get; set; } = default!;

    public virtual DateTime ViewedAtUtc { get; set; }
}
