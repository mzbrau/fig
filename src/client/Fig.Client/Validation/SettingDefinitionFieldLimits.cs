namespace Fig.Client.Validation;

/// <summary>
/// Length limits for setting metadata that is persisted to the database.
/// Unmapped NHibernate string columns default to NVARCHAR(255) (see SettingMap and SettingsClientMap).
/// </summary>
internal static class SettingDefinitionFieldLimits
{
    /// <summary>NHibernate default for unmapped string columns (see SettingMap).</summary>
    public const int StandardString = 255;
}
