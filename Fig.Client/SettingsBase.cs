using System;
using System.Linq;
using Fig.Client.Attributes;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    public abstract class SettingsBase
    {
        private readonly ISettingDefinitionFactory _settingDefinitionFactory;

        protected SettingsBase() : this(new SettingDefinitionFactory())
        {
        }
        
        protected SettingsBase(ISettingDefinitionFactory settingDefinitionFactory)
        {
            _settingDefinitionFactory = settingDefinitionFactory;
        }

        public abstract string ServiceName { get; set; }

        public abstract string ServiceSecret { get; set; }


        public SettingsDefinitionDataContract CreateDataContract()
        {
            var dataContract = new SettingsDefinitionDataContract()
            {
                ServiceName = ServiceName,
                ServiceSecret = ServiceSecret
            };
            var settingProperties = this.GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));

            var settings = settingProperties
                .Select(settingProperty => _settingDefinitionFactory.Create(settingProperty))
                .ToList();

            dataContract.Settings = settings;

            return dataContract;
        }
    }
}

