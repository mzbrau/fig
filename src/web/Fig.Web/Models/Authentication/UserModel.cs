using System.ComponentModel.DataAnnotations;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;

namespace Fig.Web.Models.Authentication;

public class UserModel
{
    private string? _originalFirstName;
    private string? _originalLastName;
    private Role _originalRole;
    private string? _originalUsername;
    private string? _originalClientFilter;
    private List<Classification>? _originalAllowedClassifications;

    public Guid? Id { get; set; }

    [Required]
    public string? Username { get; set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    public Role Role { get; set; }
    
    public string? ClientFilter { get; set; }

    public string? Password { get; set; }

    public List<Classification> AllowedClassifications { get; set; } = 
        Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();

    public string? Validate(int passwordStrength)
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName))
            return "Username, first name and last name must have values.";

        if (passwordStrength is >= 0 and < 3)
            return "Password is too weak. Minimum rating is 'Good'. See zxcvbn for detail on how password strength is calculated.";

        if (Id == null && Password == null)
            return "Password must be set.";

        if (string.IsNullOrWhiteSpace(ClientFilter))
            return "Client filter must be set. Use .* for all clients";

        return null;
    }

    public void Snapshot()
    {
        _originalUsername = Username;
        _originalFirstName = FirstName;
        _originalLastName = LastName;
        _originalRole = Role;
        _originalClientFilter = ClientFilter;
        _originalAllowedClassifications = AllowedClassifications.ToList();
    }

    public void Revert()
    {
        Username = _originalUsername;
        FirstName = _originalFirstName;
        LastName = _originalLastName;
        Role = _originalRole;
        ClientFilter = _originalClientFilter;
        AllowedClassifications = _originalAllowedClassifications ?? 
            Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();
    }
}