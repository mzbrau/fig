namespace Fig.Web.Models.Authentication;

public class UserModel
{
    public Guid Id { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
    
    public string Username { get; set; }
    public string Token { get; set; }
    
    public bool IsDeleting { get; set; }
}