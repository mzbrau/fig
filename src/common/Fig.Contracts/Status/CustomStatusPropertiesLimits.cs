namespace Fig.Contracts.Status
{
    public static class CustomStatusPropertiesLimits
    {
        public const int MaxProperties = 25;

        public const int MaxStringValueLength = 2048;

        public const int MaxPropertyNameLength = 64;

        public const int MaxSerializedJsonBytes = 4 * 1024;
    }
}
