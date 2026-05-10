namespace Fig.Web.ReleaseHighlights;

public class ReleaseHighlightsCatalog : IReleaseHighlightsCatalog
{
    private static readonly IReadOnlyList<ReleaseHighlightItem> Items = new List<ReleaseHighlightItem>
    {
        new(
            "3.0",
            "3.0-provider-defined-lookup-tables",
            "Provider Defined Lookup Tables",
            "Let clients register normal and keyed lookup tables so available options can be driven dynamically at runtime, including by another setting's value.",
            "images/release-highlights/3.0/provider-defined.png",
            10,
            "https://www.figsettings.com/docs/features/provider-defined-lookup-tables"),
        new(
            "3.0",
            "3.0-validatecount-attribute",
            "ValidateCount Attribute",
            "Validate list sizes directly with ValidateCount so collections can enforce minimums, maximums, and required item counts.",
            "images/release-highlights/3.0/validate-count.png",
            20,
            "https://www.figsettings.com/docs/features/settings-management/validation#additional-validation-attributes"),
        new(
            "3.0",
            "3.0-custom-predefined-categories",
            "Custom Predefined Categories",
            "Define reusable custom category types so settings can share strongly typed category groupings across a client.",
            "images/release-highlights/3.0/custom-categories.png",
            30,
            "https://www.figsettings.com/docs/next/features/settings-management/category/#custom-predefined-categories"),
        new(
            "3.0",
            "3.0-monaco-json-editor",
            "Monaco JSON Editor",
            "Edit JSON settings with the Monaco editor for a richer authoring experience with better structure and formatting support.",
            "images/release-highlights/3.0/json-editor.png",
            40),
        new(
            "3.0",
            "3.0-timeline-feature",
            "Timeline Feature",
            "Review a chronological timeline of client registrations and setting changes to understand what changed and when.",
            "images/release-highlights/3.0/timeline.png",
            50,
            "https://www.figsettings.com/docs/features/client-timeline"),
        new(
            "3.0",
            "3.0-dependson-attribute",
            "DependsOn Attribute",
            "Show or hide settings based on another setting's value by declaring dependencies directly in your settings model. This replaces the 'EnabledBy' attribute",
            "images/release-highlights/3.0/depends-on.png",
            70,
            "https://www.figsettings.com/docs/features/settings-management/conditional-settings"),
        new(
            "3.0",
            "3.0-setting-headings",
            "Setting Headings",
            "Add explicit headings above related settings to break large configuration surfaces into clearer sections. Note that these are automatically added based on categories.",
            "images/release-highlights/3.0/headings.png",
            90,
            "https://www.figsettings.com/docs/features/settings-management/heading"),
        new(
            "3.1",
            "3.1-aspire-integration",
            "Aspire integration",
            "Add Fig to your .NET Aspire host with dedicated extension methods and the Fig.Aspire package.",
            "images/release-highlights/3.1/aspire.png",
            10,
            "https://www.figsettings.com/docs/guides/aspire-integration"),
        new(
            "3.3",
            "3.3-client-registration-history",
            "Client registration history",
            "Review how a client's registered settings changed over time and compare against future releases.",
            "images/release-highlights/3.3/registration-history.png",
            10,
            "https://www.figsettings.com/docs/features/client-registration-history"),
        new(
            "3.3",
            "3.3-setting-compare",
            "Setting compare",
            "Import another export file or environment snapshot, inspect the differences, and selectively bring settings across.",
            "images/release-highlights/3.3/setting-compare.png",
            20,
            "https://www.figsettings.com/docs/features/compare"),
        new(
            "3.4",
            "3.4-group-value-mismatch-detection",
            "Group value mismatch detection",
            "Now if there are values within a group which are different, this will be highlighted to the user and can be resolved with a single click.",
            "images/release-highlights/3.4/groups-align.png",
            30,
            "https://www.figsettings.com/docs/features/settings-management/groups/#aligning-settings-within-a-group"),
        new(
            "3.5",
            "3.5-custom-groups",
            "Custom groups",
            "Create and manage grouped settings dynamically from the web UI instead of relying on static group definitions.",
            "images/release-highlights/3.5/groups-page.png",
            10,
            "https://www.figsettings.com/docs/features/groups"),
        new(
            "3.5",
            "3.5-information-text",
            "Dynamic information text",
            "Display scripts can now surface contextual information messages directly inside setting editors. A library of scripts is available for easy use, and {{this}} can be used to substitute the setting name",
            "images/release-highlights/3.5/information-text.png",
            20,
            "https://www.figsettings.com/docs/features/settings-management/display-scripts"),
        new(
            "3.6",
            "3.6-migrate-from-attribute",
            "MigrateFrom Attribute",
            "Rename settings safely by pointing the new setting at the previous name, allowing Fig to carry existing values and history forward during updated registration.",
            "images/release-highlights/3.6/migrate-from.png",
            10,
            "https://www.figsettings.com/docs/features/settings-management/migrate-from"),
        new(
            "3.7",
            "3.7-migrate-from-transform",
            "MigrateFrom Transformations",
            "Now, in addition to renaming settings, you can transform settings into different types or configurations. For example, convert an int into a TimeSpan or move a string into a collection of strings.",
            "images/release-highlights/3.7/migrate-from-transform.png",
            10,
            "https://www.figsettings.com/docs/features/settings-management/migrate-from"),
        new(
            "3.7",
            "3.7-client-self-updates",
            "Client Self Updates",
            "Clients can now easily update their own settings. This can be useful for client-managed passwords or setting information such as master status for leader-elected services.",
            "images/release-highlights/3.7/setting-self-update.png",
            20,
            "https://www.figsettings.com/docs/features/client-self-updates")
    };

    public IReadOnlyList<ReleaseHighlightItem> GetAll()
    {
        return Items;
    }
}
