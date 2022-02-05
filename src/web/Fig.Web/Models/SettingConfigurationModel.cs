using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;
using System.Text.RegularExpressions;

namespace Fig.Web.Models
{
    public abstract class SettingConfigurationModel
    {
        private readonly Action<SettingEvent> _stateChanged;
        private Regex _regex;
        private dynamic? _originalValue;
        private bool _isDirty;
        private bool _isValid;
        protected SettingDefinitionDataContract _definitionDataContract;

        internal SettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEvent> stateChanged)
        {
            _definitionDataContract = dataContract;
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
            _stateChanged = stateChanged;
            _originalValue = dataContract.Value;
            IsValid = true;
            if (!string.IsNullOrWhiteSpace(ValidationRegex))
            {
                _regex = new Regex(ValidationRegex, RegexOptions.Compiled);
                Validate(GetValue()?.ToString());
            }
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

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    _stateChanged(new SettingEvent(Name, SettingEventType.DirtyChanged));
                }
            }
        }

        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    _stateChanged(new SettingEvent(Name, SettingEventType.ValidChanged));
                }
            }
        }

        public void SetUpdatedSecretValue()
        {
            if (IsUpdatedSecretValueValid())
            {
                ApplyUpdatedSecretValue();
                InSecretEditMode = false;
                IsDirty = true;
            }
            else
            {
                // TODO: Show alert
            }
        }

        public void ClearDirty()
        {
            IsDirty = false;
        }

        internal abstract SettingConfigurationModel Clone(Action<SettingEvent> stateChanged);

        public void ValueChanged(string value)
        {
            IsDirty = _originalValue?.ToString() != value;
            Validate(value);
        }

        public abstract dynamic GetValue();

        protected abstract bool IsUpdatedSecretValueValid();

        protected abstract void ApplyUpdatedSecretValue();

        protected void Validate(string value)
        {
            if (_regex != null)
            {
                IsValid = _regex.IsMatch(value);
            }
        }
    }
}
