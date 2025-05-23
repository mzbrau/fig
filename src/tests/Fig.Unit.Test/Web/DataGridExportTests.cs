using NUnit.Framework; // Replaced Xunit
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fig.Contracts; // For FigPropertyType
// Stub classes are now in CsvTestDataModels.cs

namespace Fig.Unit.Test.Web
{
    [TestFixture] // Added TestFixture
    public class DataGridExportTests
    {
        // Method removed, will use CsvTestHelpers.FormatCsvField

        // Copied and adapted core logic from ExportCsv in DataGridSetting.razor
        private string GenerateCsvString(DataGridSettingModel setting)
        {
            if (setting.Value == null || setting.DataGridConfiguration?.Columns == null || !setting.DataGridConfiguration.Columns.Any())
            {
                // Handle empty or unconfigured columns case: if only columns exist, return headers.
                if (setting.DataGridConfiguration?.Columns != null && setting.DataGridConfiguration.Columns.Any())
                {
                     var headerLine = string.Join(",", setting.DataGridConfiguration.Columns.Select(c => CsvTestHelpers.FormatCsvField(c.Name)));
                     return headerLine + Environment.NewLine;
                }
                return ""; // Or handle as an error/specific output for no data and no columns
            }

            var sb = new StringBuilder();
            var columns = setting.DataGridConfiguration.Columns;

            // Headers
            sb.AppendLine(string.Join(",", columns.Select(c => CsvTestHelpers.FormatCsvField(c.Name))));

            // Data Rows
            if (setting.Value.Any()) // Ensure Value is not empty before iterating
            {
                foreach (var row in setting.Value)
                {
                    var rowValues = new List<string>();
                    foreach (var column in columns)
                    {
                        var cellValue = row.TryGetValue(column.Name, out var valueModel) ? valueModel?.ReadOnlyValue : null;
                        rowValues.Add(CsvTestHelpers.FormatCsvField(cellValue));
                    }
                    sb.AppendLine(string.Join(",", rowValues));
                }
            } else if (columns.Any()) { // No data rows, but columns exist, ensure extra newline for empty data part is not added unless intended.
                // The initial sb.AppendLine for headers already adds one newline.
                // If no data rows, this is sufficient for "headers only" CSV.
            }


            return sb.ToString();
        }

        [Test] // Changed from [Fact]
        public void GenerateCsvString_BasicExport_CorrectCsv()
        {
            var setting = new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn>
                    {
                        new DataGridColumn { Name = "Name", Type = FigPropertyType.String },
                        new DataGridColumn { Name = "Age", Type = FigPropertyType.Int }
                    }
                },
                Value = new List<Dictionary<string, IDataGridValueModel>>
                {
                    new Dictionary<string, IDataGridValueModel>
                    {
                        { "Name", new DataGridValueModel<string>("John Doe", FigPropertyType.String) },
                        { "Age", new DataGridValueModel<int>(30, FigPropertyType.Int) }
                    },
                    new Dictionary<string, IDataGridValueModel>
                    {
                        { "Name", new DataGridValueModel<string>("Jane Smith", FigPropertyType.String) },
                        { "Age", new DataGridValueModel<int>(25, FigPropertyType.Int) }
                    }
                }
            };

            var expectedCsv = "\"Name\",\"Age\"" + Environment.NewLine +
                              "\"John Doe\",\"30\"" + Environment.NewLine +
                              "\"Jane Smith\",\"25\"" + Environment.NewLine;
            
            var actualCsv = GenerateCsvString(setting);
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // NUnit constraint-based
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void GenerateCsvString_WithSpecialCharacters_CorrectlyEscapedAndQuoted()
        {
            var setting = new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn> { new DataGridColumn { Name = "Description", Type = FigPropertyType.String } }
                },
                Value = new List<Dictionary<string, IDataGridValueModel>>
                {
                    new Dictionary<string, IDataGridValueModel>
                    {
                        { "Description", new DataGridValueModel<string>("Text with \"quotes\" and ,comma", FigPropertyType.String) }
                    }
                }
            };
            // Expected: "Description"
            //           "Text with ""quotes"" and ,comma"
            var expectedCsv = "\"Description\"" + Environment.NewLine +
                              "\"Text with \"\"quotes\"\" and ,comma\"" + Environment.NewLine;
            var actualCsv = GenerateCsvString(setting);
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // Updated assertion
        }

        [Test] // Changed from [Fact]
        public void GenerateCsvString_StringListType_CorrectlyFormatted()
        {
            var setting = new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn> { new DataGridColumn { Name = "Tags", Type = FigPropertyType.StringList } }
                },
                Value = new List<Dictionary<string, IDataGridValueModel>>
                {
                    new Dictionary<string, IDataGridValueModel>
                    {
                        { "Tags", new DataGridValueModel<List<string>>(new List<string> { "tag1", "tag2", "tag with space" }, FigPropertyType.StringList) }
                    }
                }
            };
            // Expected: "Tags"
            //           "tag1,tag2,tag with space" (CsvTestHelpers.FormatCsvField joins IEnumerable<string> with comma, then quotes the whole)
            var expectedCsv = "\"Tags\"" + Environment.NewLine +
                              "\"tag1,tag2,tag with space\"" + Environment.NewLine;
            var actualCsv = GenerateCsvString(setting);
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // NUnit constraint-based
        }
        
        [Test] // Changed from [Fact]
        public void GenerateCsvString_EmptyGrid_ReturnsHeadersOnly()
        {
            var setting = new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn>
                    {
                        new DataGridColumn { Name = "Name", Type = FigPropertyType.String },
                        new DataGridColumn { Name = "Age", Type = FigPropertyType.Int }
                    }
                },
                Value = new List<Dictionary<string, IDataGridValueModel>>() // Empty data
            };

            var expectedCsv = "\"Name\",\"Age\"" + Environment.NewLine;
            var actualCsv = GenerateCsvString(setting);
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void GenerateCsvString_NullCellValues_ExportedAsEmptyQuotedString()
        {
            var setting = new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn>
                    {
                        new DataGridColumn { Name = "Name", Type = FigPropertyType.String },
                        new DataGridColumn { Name = "Value", Type = FigPropertyType.String }
                    }
                },
                Value = new List<Dictionary<string, IDataGridValueModel>>
                {
                    new Dictionary<string, IDataGridValueModel>
                    {
                        { "Name", new DataGridValueModel<string>("Test", FigPropertyType.String) },
                        // Value is intentionally missing from the dictionary, simulating a null model or value
                        // Or explicitly: { "Value", new DataGridValueModel<string>(null, FigPropertyType.String) }
                    }
                }
            };
             // For the missing "Value" column, row.TryGetValue will result in 'null' for cellValue
            var expectedCsv = "\"Name\",\"Value\"" + Environment.NewLine +
                              "\"Test\",\"\"" + Environment.NewLine; 
            var actualCsv = GenerateCsvString(setting);
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // NUnit constraint-based
        }
        
        [Test] // Changed from [Fact]
        public void GenerateCsvString_VariousDataTypes_CorrectlyFormatted()
        {
            var setting = new DataGridSettingModel
            {
                Name = "VariousTypes",
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn>
                    {
                        new DataGridColumn { Name = "ColString", Type = FigPropertyType.String },
                        new DataGridColumn { Name = "ColInt", Type = FigPropertyType.Int },
                        new DataGridColumn { Name = "ColBool", Type = FigPropertyType.Bool },
                        new DataGridColumn { Name = "ColDouble", Type = FigPropertyType.Double },
                        new DataGridColumn { Name = "ColLong", Type = FigPropertyType.Long },
                        new DataGridColumn { Name = "ColDateTime", Type = FigPropertyType.DateTime },
                        new DataGridColumn { Name = "ColTimeSpan", Type = FigPropertyType.TimeSpan }
                    }
                },
                Value = new List<Dictionary<string, IDataGridValueModel>>
                {
                    new Dictionary<string, IDataGridValueModel>
                    {
                        { "ColString", new DataGridValueModel<string>("hello", FigPropertyType.String) },
                        { "ColInt", new DataGridValueModel<int>(123, FigPropertyType.Int) },
                        { "ColBool", new DataGridValueModel<bool>(true, FigPropertyType.Bool) },
                        { "ColDouble", new DataGridValueModel<double>(123.45, FigPropertyType.Double) },
                        { "ColLong", new DataGridValueModel<long>(9876543210L, FigPropertyType.Long) },
                        { "ColDateTime", new DataGridValueModel<DateTime>(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc), FigPropertyType.DateTime) },
                        { "ColTimeSpan", new DataGridValueModel<TimeSpan>(new TimeSpan(2, 30, 0), FigPropertyType.TimeSpan) }
                    }
                }
            };

            var expectedDateString = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc).ToString();
            var expectedTimeSpanString = new TimeSpan(2, 30, 0).ToString();

            var expectedCsv = $"\"ColString\",\"ColInt\",\"ColBool\",\"ColDouble\",\"ColLong\",\"ColDateTime\",\"ColTimeSpan\"" + Environment.NewLine +
                              $"\"hello\",\"123\",\"True\",\"123.45\",\"9876543210\",\"{expectedDateString}\",\"{expectedTimeSpanString}\"" + Environment.NewLine;
            
            var actualCsv = GenerateCsvString(setting);
            Assert.That(actualCsv, Is.EqualTo(expectedCsv)); // NUnit constraint-based
        }
    }
}
