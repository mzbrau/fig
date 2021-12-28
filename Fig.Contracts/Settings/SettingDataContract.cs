using System;
using System.Diagnostics;
using Fig.Contracts.SettingTypes;

namespace Fig.Contracts.Settings
{
    public class SettingDataContract<T>: ISetting where T : SettingType
    {
        public string Name { get; set; }
        
        public object Value
        {
            get => TypedValue.Value;
            set => TypedValue = (T)Activator.CreateInstance(typeof(T), value);
        }

        public T TypedValue { get; set; }
    }
}