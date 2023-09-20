using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Exceptions;

namespace Fig.Common.NetStandard.Validation;

public class ClientNameValidator : IClientNameValidator
{
    public void Validate(string clientName)
    {
        // regular expression pattern to match reserved regex characters
        string pattern = "[\\^$.*+?(){}[\\]\\\\|]";
        
        if (string.IsNullOrWhiteSpace(clientName) || Regex.IsMatch(clientName, pattern))
            throw new InvalidClientNameException(clientName);
    }
}