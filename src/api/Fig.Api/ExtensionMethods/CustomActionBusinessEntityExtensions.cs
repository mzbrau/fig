using Fig.Contracts.CustomActions;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class CustomActionBusinessEntityExtensions
{
    public static bool Update(this CustomActionBusinessEntity customAction, CustomActionDefinitionDataContract updated)
    {
        var wasUpdated = false;
        if (customAction.ButtonName != updated.ButtonName)
        {
            customAction.ButtonName = updated.ButtonName;
            wasUpdated = true;
        }
        
        if (customAction.Description != updated.Description)
        {
            customAction.Description = updated.Description;
            wasUpdated = true;
        }
        
        if (customAction.SettingsUsed != updated.SettingsUsed)
        {
            customAction.SettingsUsed = updated.SettingsUsed;
            wasUpdated = true;
        }

        return wasUpdated;
    }
}