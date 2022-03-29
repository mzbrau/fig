using System.ComponentModel.DataAnnotations;
using Fig.Contracts.Authentication;

namespace Fig.Web.Models.Authentication;

public class UserModel
{
    private string _originalFirstName;
    private string _originalLastName;
    private Role _originalRole;
    private string _originalUsername;

    public Guid? Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public Role Role { get; set; }

    public string? Password { get; set; }

    public string? Validate(int passwordStrength)
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
            return "Username, first name and last name must have values.";

        if (passwordStrength is >= 0 and < 3)
            return "Password is too weak. Minimum rating is 'Good'.";

        if (Id == null && Password == null)
            return "Password must be set.";

        return null;
    }

    public void Snapshot()
    {
        _originalUsername = Username;
        _originalFirstName = FirstName;
        _originalLastName = LastName;
        _originalRole = Role;
    }

    public void Revert()
    {
        Username = _originalUsername;
        FirstName = _originalFirstName;
        LastName = _originalLastName;
        Role = _originalRole;
    }
}