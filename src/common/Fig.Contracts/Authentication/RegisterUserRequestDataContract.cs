using System.Collections.Generic;
using Fig.Client.Abstractions.Data;

namespace Fig.Contracts.Authentication
{
    public class RegisterUserRequestDataContract
    {
        public RegisterUserRequestDataContract(string username,
            string firstName,
            string lastName,
            Role role,
            string? password,
            string clientFilter, 
            List<Classification> allowedClassifications)
        {
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            Password = password;
            ClientFilter = clientFilter;
            AllowedClassifications = allowedClassifications;
        }

        public string Username { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public Role Role { get; set; }
        
        public string ClientFilter { get; set; }
        
        public List<Classification> AllowedClassifications { get; }

        public string? Password { get; set; }
    }
}