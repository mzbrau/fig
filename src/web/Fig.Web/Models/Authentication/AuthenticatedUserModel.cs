using Fig.Contracts.Authentication;

namespace Fig.Web.Models.Authentication;

public class AuthenticatedUserModel
{
    public Guid? Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Username { get; set; }

    public string? Token { get; set; }

    public Role Role { get; set; }
}