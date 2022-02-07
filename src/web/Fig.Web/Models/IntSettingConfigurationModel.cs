using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models
{
    public class IntSettingConfigurationModel : SettingConfigurationModel
    {
        public IntSettingConfigurationModel(SettingDefinitionDataContract dataContract, Func<SettingEventModel, Task<object>> settingEvent)
            : base(dataContract, settingEvent)
        {
            Value = dataContract.Value;
            DefaultValue = dataContract.DefaultValue;
        }

        public int Value { get; set; }

        public int DefaultValue { get; set; }


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

        protected override void SetValue(dynamic value)
        {
            Value = value;
        }

        internal override SettingConfigurationModel Clone(Func<SettingEventModel, Task<object>> stateChanged)
        {
            var clone = new IntSettingConfigurationModel(_definitionDataContract, stateChanged)
            {
                IsDirty = true
            };

            return clone;
        }
    }
}
