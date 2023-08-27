using Fig.Contracts.Attributes;

namespace Fig.Client.Validation
{
    public enum ValidationType
    {
        None,
        
        Custom,
        
        // https://stackoverflow.com/a/25969006
        [ValidationDefinition(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", 
            "Must be a valid IP Address")]
        IpAddress,
        
        [ValidationDefinition(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):(\d{1,5})$", 
            "Must be a valid IP Address and port in format <ip address>:<port>")]
        IpAddressAndPort,
        
        // https://stackoverflow.com/a/59317682
        [ValidationDefinition(@"^(?=(.*[a-z]){3,})(?=(.*[A-Z]){2,})(?=(.*[0-9]){2,})(?=(.*[!@#$%^&*()\-__+.]){1,}).{8,}$", 
            "Password must contain at least: 3 lowercase letters, 2 uppercase letters, " +
            "1 special character and be at least 8 characters long.")]
        StrongPassword,
        
        [ValidationDefinition(@"^.+$", "Value cannot be empty")]
        NotEmpty,
    }
}