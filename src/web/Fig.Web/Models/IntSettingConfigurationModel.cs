using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public class IntSettingConfigurationModel : SettingConfigurationModel
    {
        public IntSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
            : base(dataContract, valueChanged)
        {
            Value = dataContract.Value;
        }

        public int Value { get; set; }

        public int UpdatedValue { get; set; }

        public int ConfirmUpdatedValue { get; set; }


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
            return UpdatedValue == ConfirmUpdatedValue;
        }

        internal override SettingConfigurationModel Clone()
        {
            var clone = new IntSettingConfigurationModel(_definitionDataContract, _valueChanged)
            {
                IsDirty = true
            };

            return clone;
        }
    }
}
