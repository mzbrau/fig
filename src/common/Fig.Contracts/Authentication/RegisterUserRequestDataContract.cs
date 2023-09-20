namespace Fig.Contracts.Authentication
{
    public class RegisterUserRequestDataContract
    {
        public RegisterUserRequestDataContract(string username,
            string firstName,
            string lastName,
            Role role,
            string? password,
            string clientFilter)
        {
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            Password = password;
            ClientFilter = clientFilter;
        }

        public string Username { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public Role Role { get; set; }
        
        public string ClientFilter { get; set; }

        public string? Password { get; set; }
    }
}