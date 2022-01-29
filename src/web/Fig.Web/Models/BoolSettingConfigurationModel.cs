using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public class BoolSettingConfigurationModel : SettingConfigurationModel
    {
        public BoolSettingConfigurationModel()
        {

        }

        public BoolSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
            : base(dataContract, valueChanged)
        {
            Value = dataContract.Value;
            DefaultValue = dataContract.DefaultValue;
        }

        public bool Value { get; set; }

        public bool DefaultValue { get; set; }

        public bool UpdatedValue { get; set; }

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
            return true;
        }
    }
}
