using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public abstract class SettingConfigurationModel
    {
        private readonly Action<string> _valueChanged;

        internal SettingConfigurationModel()
        {
        }

        internal SettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<string> valueChanged)
        {
            Name = dataContract.Name;
            Description = dataContract.Description;
            ValidationType = dataContract.ValidationType;
            ValidationRegex = dataContract.ValidationRegex;
            ValidationExplanation = dataContract.ValidationExplanation;
            IsSecret = dataContract.IsSecret;
            Group = dataContract.Group;
            DisplayOrder = dataContract.DisplayOrder;
            _valueChanged = valueChanged;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public ValidationType ValidationType { get; set; }

        public string ValidationRegex { get; set; }

        public string ValidationExplanation { get; set; }

        public bool IsSecret { get; set; }

        public string Group { get; set; }

        public int? DisplayOrder { get; set; }

        public bool InSecretEditMode { get; set; }

        public bool IsDirty { get; set; }

        public void SetUpdatedSecretValue()
        {
            if (IsUpdatedSecretValueValid())
            {
                ApplyUpdatedSecretValue();
                InSecretEditMode = false;
                SetDirty();
            }
            else
            {
                // TODO: Show alert
            }
        }

        public void SetDirty()
        {
            IsDirty = true;
            _valueChanged(Name);
        }

        public void ClearDirty()
        {
            IsDirty = false;
        }

        public abstract dynamic GetValue();

        protected abstract bool IsUpdatedSecretValueValid();

        protected abstract void ApplyUpdatedSecretValue();
    }
}
