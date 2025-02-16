using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class UserBusinessEntityExtensionMethods
{
    public static string Details(this UserBusinessEntity user)
    {
        return $"{user.Username} ({user.FirstName} {user.LastName}) Role:{user.Role.ToString()} Classifications:{string.Join(",", user.AllowedClassifications ?? [])}";
    }
}