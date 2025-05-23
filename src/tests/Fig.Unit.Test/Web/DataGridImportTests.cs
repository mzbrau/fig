using NUnit.Framework; // Replaced Xunit
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fig.Contracts; // For FigPropertyType
// Using the same minimal model definitions as in DataGridExportTests.cs
// These would typically be in a shared test utilities project or referenced from the main project.

// (Re-using definitions from DataGridExportTests.cs for IDataGridValueModel, DataGridValueModel<T>, 
// DataGridColumn, DataGridConfigurationModel, DataGridSettingModel - these should ideally be shared)
// Stub classes are now in CsvTestDataModels.cs

namespace Fig.Unit.Test.Web
{
    [TestFixture] // Added TestFixture
    public partial class DataGridImportTests // Consolidated class
    {
        // Result structure for the import simulation
        public class ImportResult 
        {
            public bool Success { get; set; }
            public List<Dictionary<string, IDataGridValueModel>> ImportedRows { get; set; } = new List<Dictionary<string, IDataGridValueModel>>();
            public List<string> Errors { get; set; } = new List<string>();
        }

        // Adapted core logic from HandleFileSelected in DataGridSetting.razor
        private ImportResult SimulateImport(string csvContent, DataGridSettingModel setting)
        {
            var result = new ImportResult();
            var configuredColumns = setting.DataGridConfiguration?.Columns;

            if (configuredColumns == null || !configuredColumns.Any())
            {
                result.Errors.Add("Data grid is not configured for import.");
                result.Success = false;
                return result;
            }

            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (!lines.Any() || string.IsNullOrWhiteSpace(lines.First()))
            {
                result.Errors.Add("CSV file is empty or header row is missing.");
                result.Success = false;
                return result;
            }

            var headerLine = lines.First();
            List<string> csvHeaders = CsvTestHelpers.ParseCsvLine(headerLine).Select(h => h.Trim()).ToList();

            if (csvHeaders.Count != configuredColumns.Count)
            {
                result.Errors.Add($"Header column count ({csvHeaders.Count}) does not match grid configuration ({configuredColumns.Count}).");
                result.Success = false;
                return result;
            }

            for (int i = 0; i < configuredColumns.Count; i++)
            {
                if (!string.Equals(csvHeaders[i], configuredColumns[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"Header mismatch: Expected '{configuredColumns[i].Name}' but found '{csvHeaders[i]}' at column index {i}.");
                    result.Success = false;
                    return result;
                }
            }

            var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var importedRowsInternal = new List<Dictionary<string, IDataGridValueModel>>();

            for (int i = 0; i < dataLines.Count; i++)
            {
                var rowString = dataLines[i];
                List<string> cellValues = CsvTestHelpers.ParseCsvLine(rowString).Select(cv => cv.Trim()).ToList();

                if (cellValues.Count != configuredColumns.Count)
                {
                    result.Errors.Add($"Row {i + 1}: Expected {configuredColumns.Count} columns, found {cellValues.Count}.");
                    continue; 
                }

                var newRow = setting.DataGridConfiguration.CreateNewRow(); 

                for (int j = 0; j < configuredColumns.Count; j++)
                {
                    var columnConfig = configuredColumns[j];
                    var cellValueString = cellValues[j];
                    var model = newRow[columnConfig.Name];

                    try
                    {
                        if (columnConfig.IsReadOnly) { model.Value = model.ReadOnlyValue; continue; }
                        if (string.IsNullOrWhiteSpace(cellValueString) && columnConfig.Type != FigPropertyType.String && columnConfig.Type != FigPropertyType.StringList)
                        {
                            model.Value = null; continue;
                        }

                        switch (columnConfig.Type)
                        {
                            case FigPropertyType.String: model.Value = cellValueString; break;
                            case FigPropertyType.Int:
                                if (int.TryParse(cellValueString, out var intVal)) model.Value = intVal;
                                else result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Invalid integer '{cellValueString}'.");
                                break;
                            case FigPropertyType.Long:
                                if (long.TryParse(cellValueString, out var longVal)) model.Value = longVal;
                                else result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Invalid long '{cellValueString}'.");
                                break;
                            case FigPropertyType.Double:
                                if (double.TryParse(cellValueString, out var doubleVal)) model.Value = doubleVal;
                                else result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Invalid double '{cellValueString}'.");
                                break;
                            case FigPropertyType.Bool:
                                if (bool.TryParse(cellValueString, out var boolVal)) model.Value = boolVal;
                                else if (cellValueString == "1") model.Value = true;
                                else if (cellValueString == "0") model.Value = false;
                                else result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Invalid boolean '{cellValueString}'.");
                                break;
                            case FigPropertyType.DateTime:
                                if (DateTime.TryParse(cellValueString, out var dateVal)) model.Value = dateVal;
                                else result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Invalid DateTime '{cellValueString}'.");
                                break;
                            case FigPropertyType.TimeSpan:
                                if (TimeSpan.TryParse(cellValueString, out var tsVal)) model.Value = tsVal;
                                else result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Invalid TimeSpan '{cellValueString}'.");
                                break;
                            case FigPropertyType.StringList:
                                model.Value = cellValueString.Split(',').Select(s => s.Trim()).ToList();
                                break;
                            default: result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Unsupported data type '{columnConfig.Type}'."); break;
                        }
                    }
                    catch (Exception ex) { result.Errors.Add($"Row {i + 1}, Col '{columnConfig.Name}': Error parsing '{cellValueString}'. Details: {ex.Message}"); }
                }
                
                int currentErrorCount = result.Errors.Count;
                if (result.Errors.Count == currentErrorCount) 
                {
                    importedRowsInternal.Add(newRow);
                }
            }
            
            if (result.Errors.Any())
            {
                result.Success = false;
            }
            else if (!importedRowsInternal.Any() && dataLines.Any())
            {
                 result.Errors.Add("No data rows were successfully imported. Please check file content and configuration.");
                 result.Success = false; 
            }
            else
            {
                result.Success = true;
                result.ImportedRows = importedRowsInternal;
            }
            return result;
        }

        private DataGridSettingModel CreateBasicSetting()
        {
            return new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn>
                    {
                        new DataGridColumn { Name = "Name", Type = FigPropertyType.String },
                        new DataGridColumn { Name = "Age", Type = FigPropertyType.Int },
                        new DataGridColumn { Name = "Active", Type = FigPropertyType.Bool }
                    },
                    CreateNewRow = () => 
                    {
                        var row = new Dictionary<string, IDataGridValueModel>();
                        row["Name"] = new DataGridValueModel<string>(null, FigPropertyType.String);
                        row["Age"] = new DataGridValueModel<int?>(null, FigPropertyType.Int); 
                        row["Active"] = new DataGridValueModel<bool?>(null, FigPropertyType.Bool); 
                        return row;
                    }
                }
            };
        }

        // Test methods follow...
        [Test] // Changed from [Fact]
        public void SimulateImport_ValidCsv_SuccessfulImport()
        {
            var setting = CreateBasicSetting();
            var csvContent = "\"Name\",\"Age\",\"Active\"\r\n" +
                             "\"John Doe\",\"30\",\"True\"\r\n" +
                             "\"Jane Smith\",\"25\",\"false\"";

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.True, "Import should succeed. Errors: " + string.Join("; ", result.Errors));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.ImportedRows, Has.Count.EqualTo(2));
            Assert.That(result.ImportedRows[0]["Name"].Value, Is.EqualTo("John Doe"));
            Assert.That(result.ImportedRows[0]["Age"].Value, Is.EqualTo(30));
            Assert.That(result.ImportedRows[0]["Active"].Value, Is.EqualTo(true));
            Assert.That(result.ImportedRows[1]["Name"].Value, Is.EqualTo("Jane Smith"));
            Assert.That(result.ImportedRows[1]["Age"].Value, Is.EqualTo(25));
            Assert.That(result.ImportedRows[1]["Active"].Value, Is.EqualTo(false));
        }

        [Test] // Changed from [Fact]
        public void SimulateImport_InvalidHeaderCount_FailsWithError()
        {
            var setting = CreateBasicSetting();
            var csvContent = "\"Name\",\"Age\"\r\n" + // Missing "Active" header
                             "\"John Doe\",\"30\"";

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Does.Contain("Header column count (2) does not match grid configuration (3)."));
        }

        [Test] // Changed from [Fact]
        public void SimulateImport_MismatchedHeaderName_FailsWithError()
        {
            var setting = CreateBasicSetting();
            var csvContent = "\"FullName\",\"Age\",\"Active\"\r\n" + // "FullName" instead of "Name"
                             "\"John Doe\",\"30\",\"True\"";

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Does.Contain("Header mismatch: Expected 'Name' but found 'FullName' at column index 0."));
        }

        [Test] // Changed from [Fact]
        public void SimulateImport_IncorrectColumnCountInRow_AddsErrorAndSkipsRow()
        {
            var setting = CreateBasicSetting();
            var csvContent = "\"Name\",\"Age\",\"Active\"\r\n" +
                             "\"John Doe\",\"30\"\r\n" + // Missing 'Active' value
                             "\"Jane Smith\",\"25\",\"True\""; // Valid row

            var result = SimulateImport(csvContent, setting);
            
            Assert.That(result.Success, Is.False, "Import should be marked as failed due to row error.");
            Assert.That(result.Errors, Does.Contain("Row 1: Expected 3 columns, found 2."));
            Assert.That(result.ImportedRows, Is.Empty);
        }
        
        [Test] // Changed from [Fact]
        public void SimulateImport_InvalidDataType_AddsError()
        {
            var setting = CreateBasicSetting();
            var csvContent = "\"Name\",\"Age\",\"Active\"\r\n" +
                             "\"John Doe\",\"thirty\",\"True\""; // "thirty" for Age (int)

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Does.Contain("Row 1, Col 'Age': Invalid integer 'thirty'."));
            Assert.That(result.ImportedRows, Is.Empty);
        }

        [Test] // Changed from [Fact]
        public void SimulateImport_EmptyCsv_FailsWithError()
        {
            var setting = CreateBasicSetting();
            var csvContent = "";

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Does.Contain("CSV file is empty or header row is missing."));
        }

        [Test] // Changed from [Fact]
        public void SimulateImport_OnlyHeaders_SuccessNoRowsImported()
        {
             var setting = CreateBasicSetting();
            var csvContent = "\"Name\",\"Age\",\"Active\"\r\n";

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.True, "Import should succeed with no data rows. Errors: " + string.Join("; ", result.Errors));
            Assert.That(result.ImportedRows, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test] // Changed from [Fact]
        public void SimulateImport_StringListParsing_CorrectlyParsed()
        {
            var setting = new DataGridSettingModel
            {
                DataGridConfiguration = new DataGridConfigurationModel
                {
                    Columns = new List<DataGridColumn> { new DataGridColumn { Name = "Tags", Type = FigPropertyType.StringList } },
                    CreateNewRow = () => new Dictionary<string, IDataGridValueModel> { { "Tags", new DataGridValueModel<List<string>>(null, FigPropertyType.StringList) } }
                }
            };
            var csvContent = "\"Tags\"\r\n" +
                             "\"tag1,tag2, tag with space \""; 

            var result = SimulateImport(csvContent, setting);

            Assert.That(result.Success, Is.True, "Import should succeed. Errors: " + string.Join("; ", result.Errors));
            Assert.That(result.ImportedRows, Has.Count.EqualTo(1));
            var tags = result.ImportedRows[0]["Tags"].Value as List<string>;
            Assert.That(tags, Is.Not.Null);
            Assert.That(tags, Is.EqualTo(new List<string> { "tag1", "tag2", "tag with space" }));
        }
    }
}
