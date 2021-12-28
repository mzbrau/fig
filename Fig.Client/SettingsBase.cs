using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client.Attributes;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    public abstract class SettingsBase
    {
        public abstract string ServiceName { get; set; }

        public abstract string ServiceSecret { get; set; }


        public SettingsDefinitionDataContract ToDataContract()
        {
            var dataContract = new SettingsDefinitionDataContract();
            var settings = new List<ISettingDefinition>();
            var settingProperties = this.GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));

            foreach (var setting in settingProperties)
            {
            }

            return dataContract;
        }
    }
}

