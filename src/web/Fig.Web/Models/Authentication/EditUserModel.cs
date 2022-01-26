using System.ComponentModel.DataAnnotations;

namespace Fig.Web.Models.Authentication;

public class EditUserModel
{
    public EditUserModel()
    {
    }

    public EditUserModel(UserModel user)
    {
        FirstName = user.FirstName;
        LastName = user.LastName;
        Username = user.Username;
    }
    
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string Username { get; set; }
    
    public string Password { get; set; }
}