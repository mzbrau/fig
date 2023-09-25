using System;

namespace Fig.Contracts.Authentication
{
    public class UserDataContract
    {
        public UserDataContract(Guid id,
            string username,
            string firstName,
            string lastName,
            Role role,
            string clientFilter)
        {
            Id = id;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            ClientFilter = clientFilter;
        }

        public Guid Id { get; }
    
        public string Username { get; }
    
        public string FirstName { get; }
    
        public string LastName { get; }
    
        public Role Role { get; }
        
        public string ClientFilter { get; }
    }
}