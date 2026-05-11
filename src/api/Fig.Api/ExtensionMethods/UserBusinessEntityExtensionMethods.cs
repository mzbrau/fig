using Fig.Api.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class UserBusinessEntityExtensionMethods
{
    public static string Details(this UserBusinessEntity user)
    {
        return $"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role.ToString()} Classifications:{string.Join(",", user.AllowedClassifications ?? [])} PasswordChangeRequired:{user.PasswordChangeRequired}";
    }

    public static bool RequiresPasswordChange(this UserBusinessEntity user, ApiSettings apiSettings)
    {
        return user.PasswordChangeRequired ||
               (apiSettings.ForceAdminDefaultPasswordChange &&
                user.Username == DefaultUser.UserName &&
                BCrypt.Net.BCrypt.EnhancedVerify(DefaultUser.Password, user.PasswordHash));
    }
}
