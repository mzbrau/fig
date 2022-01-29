using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using System.Text.RegularExpressions;

namespace Fig.Web.Models
{
    public abstract class SettingConfigurationModel
    {
        private readonly Action<bool, string> _valueChanged;
        private Lazy<Regex>? _regex;
        private dynamic? _originalValue;

        internal SettingConfigurationModel()
        {
        }

        internal SettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
        {
            Name = dataContract.Name;
            Description = dataContract.Description;
            ValidationType = dataContract.ValidationType;
            ValidationRegex = dataContract.ValidationRegex;
            ValidationExplanation = string.IsNullOrWhiteSpace(dataContract.ValidationExplanation) ?
                $"Did not match validation regex ({ValidationRegex})" :
                dataContract.ValidationExplanation;
            IsSecret = dataContract.IsSecret;
            Group = dataContract.Group;
            DisplayOrder = dataContract.DisplayOrder;
            _valueChanged = valueChanged;
            _originalValue = dataContract.Value;
            IsValid = true;
            if (!string.IsNullOrWhiteSpace(ValidationRegex))
                _regex = new Lazy<Regex>(() => new Regex(ValidationRegex, RegexOptions.Compiled));
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

        public bool IsValid { get; set; }

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

        }

        public void ClearDirty()
        {
            IsDirty = false;
        }

        public void ValueChanged(string value)
        {
            IsDirty = _originalValue?.ToString() != value;
            _valueChanged(IsDirty, Name);
            Validate(value);
        }

        public abstract dynamic GetValue();

        protected abstract bool IsUpdatedSecretValueValid();

        protected abstract void ApplyUpdatedSecretValue();

        protected void Validate(string value)
        {
            if (_regex != null)
            {
                IsValid = _regex.Value.IsMatch(value);
            }
        }
    }
}
