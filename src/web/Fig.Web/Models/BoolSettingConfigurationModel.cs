using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models
{
    public class BoolSettingConfigurationModel : SettingConfigurationModel
    {
        public BoolSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEvent> stateChanged)
            : base(dataContract, stateChanged)
        {
            Value = dataContract.Value;
            DefaultValue = dataContract.DefaultValue ?? false;
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

        internal override SettingConfigurationModel Clone(Action<SettingEvent> stateChanged)
        {
            var clone = new BoolSettingConfigurationModel(_definitionDataContract, stateChanged)
            {
                IsDirty = true
            };

            return clone;
        }
    }
}
