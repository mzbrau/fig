namespace Fig.Web.Models.Compare;

public enum CompareStatus
{
    /// <summary>Setting value matches the export.</summary>
    Match,

    /// <summary>Setting value differs from the export.</summary>
    Different,

    /// <summary>Setting exists in Fig but not in the export.</summary>
    OnlyInLive,

    /// <summary>Setting exists in the export but not in live.</summary>
    OnlyInExport,

    /// <summary>Setting is secret and was not compared.</summary>
    NotCompared
}
