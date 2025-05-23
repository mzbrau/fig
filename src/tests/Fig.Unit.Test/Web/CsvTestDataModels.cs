using System;
using System.Collections.Generic;
using Fig.Contracts; // For FigPropertyType

namespace Fig.Unit.Test.Web
{
    // Minimal interface to allow testing.
    public interface IDataGridValueModel 
    {
        object Value { get; set; }
        object ReadOnlyValue { get; } 
        FigPropertyType Type { get; }
        bool IsSecret { get; }
        bool IsReadOnly { get; }
    }

    public class DataGridValueModel<T> : IDataGridValueModel
    {
        public T InternalValue { get; set; }
        public object Value { get => InternalValue; set => InternalValue = (T)value; }
        public object ReadOnlyValue => InternalValue;
        public FigPropertyType Type { get; private set; }
        public bool IsSecret { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;

        public DataGridValueModel(T val, FigPropertyType type)
        {
            InternalValue = val;
            Type = type;
        }
    }

    public class DataGridColumn
    {
        public string Name { get; set; }
        public FigPropertyType Type { get; set; }
        public bool IsReadOnly { get; set; } // Added to match potential usage in import logic
    }

    public partial class DataGridConfigurationModel // Made partial here for the Func
    {
        public List<DataGridColumn> Columns { get; set; } = new List<DataGridColumn>();
        public Func<Dictionary<string, IDataGridValueModel>> CreateNewRow { get; set; }
    }

    public class DataGridSettingModel // Represents the 'Setting' property
    {
        public string Name { get; set; } = "TestSetting";
        public List<Dictionary<string, IDataGridValueModel>> Value { get; set; } = new List<Dictionary<string, IDataGridValueModel>>();
        public DataGridConfigurationModel DataGridConfiguration { get; set; } = new DataGridConfigurationModel();
    }
}
