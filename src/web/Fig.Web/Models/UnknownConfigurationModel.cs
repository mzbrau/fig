using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models
{
    public class UnknownConfigurationModel : SettingConfigurationModel
    {
        public UnknownConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEvent> stateChanged)
            : base(dataContract, stateChanged)
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

        internal override SettingConfigurationModel Clone(Action<SettingEvent> stateChanged)
        {
            return new UnknownConfigurationModel(_definitionDataContract, stateChanged);
        }
    }
}
