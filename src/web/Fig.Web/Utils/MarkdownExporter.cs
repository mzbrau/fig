using System.Text;
using Fig.Contracts.ImportExport;

namespace Fig.Web.Utils;

public class MarkdownExporter : IMarkdownExporter
{
    public string CreateMarkdown(FigDataExportDataContract export, bool maskSecrets)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Fig Setting Report");
        builder.AppendLine();
        builder.AppendLine($"**Date:** {export.ExportedAt.ToString("f")}");
        builder.AppendLine();
        builder.AppendLine($"**Clients:** {export.Clients.Count}");
        builder.AppendLine();
        builder.AppendLine($"**Settings:** {export.Clients.SelectMany(a => a.Settings).Count()}");
        builder.AppendLine();

        foreach (var client in export.Clients)
        {
            builder.AppendLine($"[{client.Name}](#{client.Name.Replace(" ", "")})");
            builder.AppendLine();
        }

        foreach (var client in export.Clients)
        {
            builder.AppendLine(CreateClient(client, maskSecrets));
            builder.AppendLine();
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string CreateClient(SettingClientExportDataContract client, bool maskSecrets)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"<a name=\"{client.Name.Replace(" ", "")}\"/>");
        builder.AppendLine();
        builder.AppendLine($"## {client.Name}");
        builder.AppendLine();
        if (client.Instance != null)
        {
            builder.AppendLine($"**Instance**: {client.Instance}");
            builder.AppendLine();
        }
            
        foreach (var setting in client.Settings.OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name))
        {
            builder.AppendLine(CreateSetting(setting, maskSecrets));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string CreateSetting(SettingExportDataContract setting, bool maskSecrets)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"### {setting.Name}");
        builder.AppendLine();
        builder.AppendLine($"  {setting.Description}");
        builder.AppendLine();
        builder.AppendLine($"  *Type*: {setting.ValueType.Name}");
        builder.AppendLine();

        if (setting.DefaultValue != null)
        {
            builder.AppendLine($"  *Default*: {setting.DefaultValue}");
            builder.AppendLine();
        }

        if (setting.IsSecret && maskSecrets)
        {
            builder.AppendLine($"  *Value*: ******");
        }
        else
        {
            builder.AppendLine($"  *Value*: {setting.Value}");
        }

        builder.AppendLine();
        return builder.ToString();
    }
}