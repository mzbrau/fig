namespace Fig.Contracts.SettingTypes
{
    public class IntType : SettingType
    {
        public IntType(long value)
        {
            Value = value;
        }

        public sealed override object Value { get; set; }
    }
}