using Fig.Contracts.Settings;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public static class ColumnWidthHelper
{
    public static Dictionary<string, string> GetColumnWidths(DataGridSettingDataContract? setting)
    {
        var table = setting?.Value;

        if (table is null)
            return new Dictionary<string, string>();

        var maxColumnSizes = GetMaxLengthPerColumn(table);
        return GetColumnPercentageSizes(maxColumnSizes);
    }

    private static Dictionary<string, string> GetColumnPercentageSizes(Dictionary<string, int> maxColumnSizes)
    {
        var columnPercentageSizes = new Dictionary<string, string>();
        double totalCharacters = maxColumnSizes.Values.Sum();
        totalCharacters += 0.06 * totalCharacters; // add some space for the buttons
        foreach (var kvp in maxColumnSizes)
        {
            var columnName = kvp.Key;
            var maxChars = kvp.Value;
            var percentage = (double)100 / totalCharacters * maxChars;
            percentage = Math.Max(percentage, 8); // minimum 8% width 
            columnPercentageSizes[columnName] = $"{percentage}%";
        }

        return columnPercentageSizes;
    }

    private static Dictionary<string, int> GetMaxLengthPerColumn(List<Dictionary<string, object?>> table)
    {
        var maxColumnSizes = new Dictionary<string, int>();
        foreach (var row in table)
        {
            foreach (var (columnName, value) in row)
            {
                if (value != null)
                {
                    var lines = value.ToString()?.Split('\n') ?? [];
                    var length = lines.Max(line => line.Length);
                    if (!maxColumnSizes.ContainsKey(columnName) || length > maxColumnSizes[columnName])
                    {
                        maxColumnSizes[columnName] = length;
                    }
                }
            }
        }

        return maxColumnSizes;
    }
}
