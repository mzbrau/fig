namespace Fig.Contracts.Authentication
{
    public class AuthenticateRequestDataContract
    {
        public AuthenticateRequestDataContract(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}