namespace Fig.Web.Models.Authentication;

public class LoginModel
{
    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }
}