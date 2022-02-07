using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models
{
    public class UnknownConfigurationModel : SettingConfigurationModel
    {
        public UnknownConfigurationModel(SettingDefinitionDataContract dataContract, Func<SettingEventModel, Task<object>> settingEvent)
            : base(dataContract, settingEvent)
        {
        }

        public override dynamic GetValue()
        {
            return "Not implemented";
        }

        protected override void ApplyUpdatedSecretValue()
        {

        }

        protected override bool IsUpdatedSecretValueValid()
        {
            return true;
        }

        protected override void SetValue(dynamic value)
        {

        }

        internal override SettingConfigurationModel Clone(Func<SettingEventModel, Task<object>> stateChanged)
        {
            return new UnknownConfigurationModel(_definitionDataContract, stateChanged);
        }
    }
}
