namespace Fig.Contracts.Authentication
{
    public class RegisterUserRequestDataContract
    {
        public string Username { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public Role Role { get; set; }

        public string Password { get; set; }
    }
}