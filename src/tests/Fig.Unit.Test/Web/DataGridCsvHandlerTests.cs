using System;
using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Utils;
using Fig.Web.Scripting;
using NUnit.Framework;
using Moq;

namespace Fig.Unit.Test.Web
{
    [TestFixture]
    public class DataGridCsvHandlerTests
    {
        [Test]
        public void ConvertToCsv_EmptyOrNull_ReturnsNull()
        {
            var def = MakeDataGridDefinition(["A"], [typeof(string)]);
            var setting = new DataGridSettingConfigurationModel(def, MakeClientConfig(), MakePresentation())
                {
                    Value = null
                };
            Assert.That(DataGridCsvHandler.ConvertToCsv(setting), Is.Null);
            setting.Value = new List<Dictionary<string, IDataGridValueModel>>();
            Assert.That(DataGridCsvHandler.ConvertToCsv(setting), Is.Null);
        }

        [Test]
        public void ConvertToCsv_ValidRows_ProducesCsv()
        {
            var def = MakeDataGridDefinition(["A", "B"], [typeof(string), typeof(int)]);
            var setting = new DataGridSettingConfigurationModel(def, MakeClientConfig(), MakePresentation())
                {
                    Value =
                    [
                        new()
                        {
                            { "A", new DataGridValueModel<string>("foo", false, MakeMockSetting()) },
                            { "B", new DataGridValueModel<int>(42, false, MakeMockSetting()) },
                        }
                    ]
                };
            var csv = DataGridCsvHandler.ConvertToCsv(setting);
            Assert.That(csv, Does.Contain("\"A\",\"B\""));
            Assert.That(csv, Does.Contain("\"foo\",\"42\""));
        }

        [Test]
        public void ParseCsvLine_HandlesQuotesAndCommas()
        {
            var line = "a,b,\"c,d\",\"e\"\"f\"\"";
            var fields = DataGridCsvHandler.ParseCsvLine(line);
            Assert.That(fields.Count, Is.EqualTo(4));
            Assert.That(fields[2], Is.EqualTo("c,d"));
            Assert.That(fields[3], Is.EqualTo("e\"f\""));
        }
        
        [Test]
        public void ParseCsvLine_HandlesUnquotedData()
        {
            var line = "a,b,c,d,e,f";
            var fields = DataGridCsvHandler.ParseCsvLine(line);
            Assert.That(fields.Count, Is.EqualTo(6));
        }

        [Test]
        public void ParseCsvToRows_EmptyOrHeaderOnly_ReturnsError()
        {
            var columns = new List<DataGridColumn> { MakeColumn("A", typeof(string)) };
            var result = DataGridCsvHandler.ParseCsvToRows("", columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors, Is.Not.Empty);
            result = DataGridCsvHandler.ParseCsvToRows("A", columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Rows, Is.Empty);
        }

        [Test]
        public void ParseCsvToRows_HeaderMismatch_ReturnsError()
        {
            var columns = new List<DataGridColumn> { MakeColumn("A", typeof(string)), MakeColumn("B", typeof(int)) };
            var result = DataGridCsvHandler.ParseCsvToRows("A,C", columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void ParseCsvToRows_RowColumnCountMismatch_ReturnsError()
        {
            var columns = new List<DataGridColumn> { MakeColumn("A", typeof(string)), MakeColumn("B", typeof(int)) };
            var csv = "A,B\nfoo";
            var result = DataGridCsvHandler.ParseCsvToRows(csv, columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void ParseCsvToRows_ParsesAllSupportedTypes()
        {
            var columns = new List<DataGridColumn>
            {
                MakeColumn("S", typeof(string)),
                MakeColumn("I", typeof(int)),
                MakeColumn("L", typeof(long)),
                MakeColumn("D", typeof(double)),
                MakeColumn("B", typeof(bool)),
                MakeColumn("DT", typeof(DateTime)),
                MakeColumn("TS", typeof(TimeSpan)),
                MakeColumn("LS", typeof(List<string>)),
            };
            var dt = DateTime.Now;
            var ts = TimeSpan.FromMinutes(5);
            var csv = $"S,I,L,D,B,DT,TS,LS\nstr,1,2,3.5,true,{dt},{ts},\"foo,bar,baz\"";
            var result = DataGridCsvHandler.ParseCsvToRows(csv, columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rows.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParseCsvToRows_InvalidTypeValues_ReturnsError()
        {
            var columns = new List<DataGridColumn>
            {
                MakeColumn("I", typeof(int)),
                MakeColumn("B", typeof(bool)),
                MakeColumn("D", typeof(double)),
                MakeColumn("DT", typeof(DateTime)),
                MakeColumn("TS", typeof(TimeSpan)),
            };
            var csv = "I,B,D,DT,TS\nnotint,notbool,notdouble,notdate,notspan";
            var result = DataGridCsvHandler.ParseCsvToRows(csv, columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors.Count, Is.EqualTo(5));
        }

        [Test]
        public void ParseCsvToRows_ReadOnlyColumn_SetsNull()
        {
            var columns = new List<DataGridColumn> { MakeColumn("A", typeof(string), isReadOnly: true) };
            var csv = "A\nfoo";
            var result = DataGridCsvHandler.ParseCsvToRows(csv, columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Rows[0]["A"].ReadOnlyValue, Is.Null);
        }

        [Test]
        public void ParseCsvToRows_ListString_ParsesList()
        {
            var columns = new List<DataGridColumn> { MakeColumn("LS", typeof(List<string>)) };
            var csv = "LS\n\"foo,bar,baz\"";
            var result = DataGridCsvHandler.ParseCsvToRows(csv, columns, DummyCreateValueModel, MakeMockSetting());
            Assert.That(result.Errors, Is.Empty);
            var list = result.Rows[0]["LS"].ReadOnlyValue as List<string>;
            Assert.That(list, Is.Not.Null);
            Assert.That(list, Has.Count.EqualTo(3));
            Assert.That(list, Contains.Item("foo"));
            Assert.That(list, Contains.Item("bar"));
            Assert.That(list, Contains.Item("baz"));
        }

        private static ISetting MakeMockSetting()
        {
            var mock = new Mock<ISetting>();
            mock.SetupAllProperties();
            mock.SetupGet(x => x.Name).Returns("Dummy");
            mock.SetupGet(x => x.DisplayName).Returns("Dummy");
            mock.SetupGet(x => x.Description).Returns(new Microsoft.AspNetCore.Components.MarkupString(""));
            mock.SetupGet(x => x.IsSecret).Returns(false);
            mock.SetupGet(x => x.IsValid).Returns(true);
            mock.SetupGet(x => x.ValueType).Returns(typeof(object));
            // Add more setups as needed for your tests
            return mock.Object;
        }
        
        private static DataGridColumn MakeColumn(string name, Type type, bool isReadOnly = false) =>
            new(name, type, null, null, isReadOnly, null, null, false, "100px");

        private static SettingClientConfigurationModel MakeClientConfig()
        {
            return new SettingClientConfigurationModel(
                name: "Test",
                description: "desc",
                instance: null,
                hasDisplayScripts: false,
                scriptRunner: Mock.Of<IScriptRunner>(),
                isGroup: false);
        }

        private static IDataGridValueModel DummyCreateValueModel(Type type, object? value, DataGridColumn col, ISetting parent)
        {
            var genericType = typeof(DataGridValueModel<>).MakeGenericType(type);
            return (IDataGridValueModel?)Activator.CreateInstance(genericType, value, col.IsReadOnly, parent, col.ValidValues, col.EditorLineCount, col.ValidationRegex, col.ValidationExplanation, col.IsSecret)
                ?? throw new Exception("Failed to create value model");
        }

        // Helper to create a minimal SettingDefinitionDataContract for tests
        private static SettingDefinitionDataContract MakeDataGridDefinition(string[] columnNames, Type[] types)
        {
            var columns = new List<DataGridColumnDataContract>();
            for (int i = 0; i < columnNames.Length; i++)
                columns.Add(new DataGridColumnDataContract(columnNames[i], types[i]));
            return new SettingDefinitionDataContract(
                name: "TestSetting",
                description: "desc",
                value: null,
                isSecret: false,
                valueType: typeof(object),
                defaultValue: null,
                validationRegex: null,
                validationExplanation: null,
                validValues: null,
                group: null,
                displayOrder: null,
                advanced: false,
                lookupTableKey: null,
                editorLineCount: null,
                jsonSchema: null,
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)
            );
        }

        // Helper to create a minimal SettingPresentation for tests
        private static SettingPresentation MakePresentation() => new(false);
    }
}
