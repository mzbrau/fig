using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;
using System.Text.RegularExpressions;

namespace Fig.Web.Models
{
    public abstract class SettingConfigurationModel
    {
        private readonly Action<SettingEventArgs> _stateChanged;
        private Regex _regex;
        private dynamic? _originalValue;
        private bool _isDirty;
        private bool _isValid;
        protected SettingDefinitionDataContract _definitionDataContract;

        internal SettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEventArgs> stateChanged)
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
            _isValid = true;
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

        public bool IsHistoryVisible { get; set; } = false;

        public bool ResetToDefaultDisabled => _definitionDataContract.DefaultValue == null ||
                                                GetValue() == _definitionDataContract.DefaultValue;

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    _stateChanged(new SettingEventArgs(Name, SettingEventType.DirtyChanged));
                }
            }
        }

        public bool IsNotDirty => !IsDirty;

        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    _stateChanged(new SettingEventArgs(Name, SettingEventType.ValidChanged));
                }
            }
        }

        public List<SettingHistoryModel> History { get; set; }

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

        public void MarkAsSaved()
        {
            IsDirty = false;
            _originalValue = GetValue();
        }

        internal abstract SettingConfigurationModel Clone(Action<SettingEventArgs> stateChanged);

        public void ValueChanged(string value)
        {
            IsDirty = _originalValue?.ToString() != value;
            Validate(value);
        }

        public void UndoChanges()
        {
            SetValue(_originalValue);
            ValueChanged(GetValue().ToString());
        }

        public void ShowHistory()
        {
            IsHistoryVisible = !IsHistoryVisible;

            if (IsHistoryVisible)
            {
                var settingEvent = new SettingEventArgs(Name, SettingEventType.HistoryRequested);
                _stateChanged(settingEvent);
                if (settingEvent.CallbackData is List<SettingHistoryModel> history)
                {
                    History = history;
                }
            }
        }

        public abstract dynamic GetValue();

        public void ResetToDefault()
        {
            if (_definitionDataContract.DefaultValue != null)
            {
                SetValue(_definitionDataContract.DefaultValue);
                ValueChanged(GetValue().ToString());
            }
        }

        protected abstract bool IsUpdatedSecretValueValid();

        protected abstract void ApplyUpdatedSecretValue();

        protected abstract void SetValue(dynamic value);

        protected void Validate(string value)
        {
            if (_regex != null)
            {
                IsValid = _regex.IsMatch(value);
            }
        }
    }
}
