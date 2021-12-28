using Fig.Contracts.SettingTypes;

namespace Fig.Contracts.Settings
{
    public class SettingDataContract<T>: ISetting where T : SettingType
    {
        public string Name { get; set; }
        
        public object Value
        {
            get => TypedValue;
            set => TypedValue = value as T;
        }

        public T TypedValue { get; set; }
    }
}