using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public class StringSettingConfigurationModel : SettingConfigurationModel
    {
        public StringSettingConfigurationModel()
        {

        }

        public StringSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
            : base(dataContract, valueChanged)
        {
            Value = dataContract.Value;
            DefaultValue = dataContract.DefaultValue;
        }

        public string Value { get; set; }

        public string DefaultValue { get; set; }

        public string UpdatedValue { get; set; }

        public string ConfirmUpdatedValue { get; set; }

        public override dynamic GetValue()
        {
            return Value;
        }

        protected override void ApplyUpdatedSecretValue()
        {
            Value = UpdatedValue;
        }

        protected override bool IsUpdatedSecretValueValid()
        {
            return !string.IsNullOrWhiteSpace(UpdatedValue) &&
                    UpdatedValue == ConfirmUpdatedValue;
        }
    }
}
