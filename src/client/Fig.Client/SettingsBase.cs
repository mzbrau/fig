using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client
{
    public abstract class SettingsBase
    {
        private readonly ISettingDefinitionFactory _settingDefinitionFactory;

        protected SettingsBase() : this(new SettingDefinitionFactory())
        {
        }

        protected SettingsBase(ISettingDefinitionFactory settingDefinitionFactory, IEnumerable<SettingDataContract> settings = null)
        {
            _settingDefinitionFactory = settingDefinitionFactory;
            if (settings != null)
            {
                SetPropertiesFromSettings(settings.ToList());
            }
            else
            {
                SetPropertiesFromDefaultValues();
            }
        }

        public abstract string ClientName { get; }

        public abstract string ClientSecret { get; }

        
        public SettingsClientDefinitionDataContract CreateDataContract()
        {
            var dataContract = new SettingsClientDefinitionDataContract()
            {
                Instance = null, // TODO
                Name = ClientName,
            };
            
            var settings = GetSettingProperties()
                .Select(settingProperty => _settingDefinitionFactory.Create(settingProperty))
                .ToList();

            dataContract.Settings = settings;

            return dataContract;
        }
        
        private IEnumerable<PropertyInfo> GetSettingProperties() => GetType().GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));

        private void SetPropertiesFromDefaultValues()
        {
            foreach (var property in GetSettingProperties())
            {
                SetDefaultValue(property);
            }
        }

        private void SetDefaultValue(PropertyInfo property)
        {
            if (property.GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType() == typeof(DefaultValueAttribute)) is DefaultValueAttribute defaultValue)
            {
                property.SetValue(this, defaultValue.Value);
            }
        }

        private void SetPropertiesFromSettings(List<SettingDataContract> settings)
        {
            foreach (var property in GetSettingProperties())
            {
                var definition = settings.FirstOrDefault(a => a.Name == property.Name);

                if (definition != null)
                {
                    property.SetValue(this, definition.Value);
                }
                else
                {
                    SetDefaultValue(property);
                }
            }
        }
    }
}

