using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public class UnknownConfigurationModel : SettingConfigurationModel
    {
        public UnknownConfigurationModel(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
            : base(dataContract, valueChanged)
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

        internal override SettingConfigurationModel Clone()
        {
            return new UnknownConfigurationModel(_definitionDataContract, _valueChanged);
        }
    }
}
