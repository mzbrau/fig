namespace Fig.Contracts.Status
{
    public class CustomStatusPropertyDataContract
    {
        public CustomStatusPropertyDataContract()
        {
        }

        public CustomStatusPropertyDataContract(
            string name,
            CustomStatusValueType valueType,
            object? value,
            string? displayName = null,
            string? enumTypeName = null,
            bool highlight = false,
            bool showInUi = true,
            int order = 0,
            string? textColor = null)
        {
            Name = name;
            ValueType = valueType;
            Value = value;
            DisplayName = displayName;
            EnumTypeName = enumTypeName;
            Highlight = highlight;
            ShowInUi = showInUi;
            Order = order;
            TextColor = textColor;
        }

        public string Name { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public CustomStatusValueType ValueType { get; set; }

        public object? Value { get; set; }

        public string? EnumTypeName { get; set; }

        public bool Highlight { get; set; }

        public bool ShowInUi { get; set; } = true;

        public int Order { get; set; }

        /// <summary>
        /// Optional CSS text color for Fig.Web value display. Format: #RGB or #RRGGBB.
        /// </summary>
        public string? TextColor { get; set; }
    }
}
