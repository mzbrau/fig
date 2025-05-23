using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fig.Contracts; // Required for FigPropertyType if used by FormatCsvField, not directly but good for context

namespace Fig.Unit.Test.Web
{
    public static class CsvTestHelpers
    {
        // Returns list of strings, where quoted fields have quotes removed and escaped quotes unescaped.
        // Unquoted fields are returned as-is. No trimming is done by this function.
        public static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            if (line == null) return fields;

            var currentField = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Check for escaped quote: ""
                        if (i + 1 < line.Length && line[i+1] == '"') 
                        {
                            currentField.Append('"');
                            i++; // Skip next quote
                        }
                        else // End of quoted field
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c); // Character inside quotes
                    }
                }
                else // Not in quotes
                {
                    if (c == '"' && currentField.Length == 0) // Start of a new quoted field
                    {
                        inQuotes = true;
                    }
                    else if (c == ',') // Delimiter
                    {
                        fields.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else // Character for unquoted field, or quote not at start of field
                    {
                        currentField.Append(c);
                    }
                }
            }
            fields.Add(currentField.ToString()); // Add the last field

            return fields;
        }

        public static string FormatCsvField(object field)
        {
            if (field == null) return "\"\""; 

            string valueString;
            if (field is IEnumerable<string> list)
            {
                valueString = string.Join(",", list);
            }
            else
            {
                valueString = field.ToString();
            }
            
            valueString = valueString.Replace("\"", "\"\"");
            return $"\"{valueString}\"";
        }
    }
}
