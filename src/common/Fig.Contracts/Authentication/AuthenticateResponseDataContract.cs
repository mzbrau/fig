using System;
using System.Collections.Generic;
using Fig.Common.NetStandard.Data;

namespace Fig.Contracts.Authentication
{
    public class AuthenticateResponseDataContract
    {
        public AuthenticateResponseDataContract(Guid id, string username, string firstName, string lastName, Role role,
            string token, bool passwordChangeRequired, List<Classification> allowedClassifications)
        {
            Id = id;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            Token = token;
            PasswordChangeRequired = passwordChangeRequired;
            AllowedClassifications = allowedClassifications;
        }

        public Guid Id { get; }
        
        public string Username { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }
        
        public Role Role { get; }

        public string Token { get; }

        public bool PasswordChangeRequired { get; }
        
        public List<Classification> AllowedClassifications { get; }
    }
}