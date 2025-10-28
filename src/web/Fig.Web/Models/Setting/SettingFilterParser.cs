using System.Text.RegularExpressions;

namespace Fig.Web.Models.Setting;

public class SettingFilterParser
{
    private static readonly Regex FilterPrefixRegex = new(
        @"^(advanced|category|secret|valid|modified|classification):",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SettingFilterCriteria Parse(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return new SettingFilterCriteria();

        var criteria = new SettingFilterCriteria();
        var parts = filter.Split([' '], StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var match = FilterPrefixRegex.Match(part);
            if (match.Success)
            {
                var prefix = match.Groups[1].Value.ToLowerInvariant();
                var value = part.Substring(match.Length);

                switch (prefix)
                {
                    case "advanced":
                        if (TryParsePartialBool(value, out var advancedValue))
                            criteria.Advanced = advancedValue;
                        break;
                    case "category":
                        criteria.Category = value;
                        break;
                    case "secret":
                        if (TryParsePartialBool(value, out var secretValue))
                            criteria.Secret = secretValue;
                        break;
                    case "valid":
                        if (TryParsePartialBool(value, out var validValue))
                            criteria.Valid = validValue;
                        break;
                    case "classification":
                        criteria.Classification = value;
                        break;
                    case "modified":
                        if (TryParsePartialBool(value, out var modifiedValue))
                            criteria.Modified = modifiedValue;
                        break;
                }
            }
            else
            {
                // No prefix, treat as general search term
                criteria.GeneralSearchTerms.Add(part);
            }
        }

        return criteria;
    }
    
    private static bool TryParsePartialBool(string value, out bool result)
    {
        result = false;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        var lowerValue = value.ToLowerInvariant();
        
        // Try exact boolean parse first
        if (bool.TryParse(lowerValue, out result))
            return true;
        
        // Try partial match for "true"
        if ("true".StartsWith(lowerValue, StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }
        
        // Try partial match for "false"
        if ("false".StartsWith(lowerValue, StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }
        
        return false;
    }
}
