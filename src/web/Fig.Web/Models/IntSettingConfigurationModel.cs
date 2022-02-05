using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models
{
    public class IntSettingConfigurationModel : SettingConfigurationModel
    {
        public IntSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEvent> stateChanged)
            : base(dataContract, stateChanged)
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

        internal override SettingConfigurationModel Clone(Action<SettingEvent> stateChanged)
        {
            var clone = new IntSettingConfigurationModel(_definitionDataContract, stateChanged)
            {
                IsDirty = true
            };

            return clone;
        }
    }
}
