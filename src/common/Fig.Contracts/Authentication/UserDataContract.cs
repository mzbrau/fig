using System;
using System.Collections.Generic;
using Fig.Client.Abstractions.Data;

namespace Fig.Contracts.Authentication
{
    public class UserDataContract
    {
        public UserDataContract(Guid id,
            string username,
            string firstName,
            string lastName,
            Role role,
            string clientFilter,
            List<Classification> allowedClassifications,
            bool passwordChangeRequired = false)
        {
            Id = id;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            ClientFilter = clientFilter;
            AllowedClassifications = allowedClassifications;
            PasswordChangeRequired = passwordChangeRequired;
        }

        public Guid Id { get; }
    
        public string Username { get; }
    
        public string FirstName { get; }
    
        public string LastName { get; }
    
        public Role Role { get; }
        
        public string ClientFilter { get; }
        
        public List<Classification> AllowedClassifications { get; }

        public bool PasswordChangeRequired { get; set; }
    }
}
