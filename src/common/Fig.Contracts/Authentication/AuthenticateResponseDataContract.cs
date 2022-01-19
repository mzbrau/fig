using System;

namespace Fig.Contracts.Authentication
{
    public class AuthenticateResponseDataContract
    {
        public Guid Id { get; set; }
        
        public string Username { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public Role Role { get; set; }

        public string Token { get; set; }
    }
}