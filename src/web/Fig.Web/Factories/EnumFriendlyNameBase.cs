using System.Text.RegularExpressions;

namespace Fig.Web.Factories;

public abstract class EnumFriendlyNameBase<T> where T : Enum
{
    private readonly Regex _camelCaseAddSpaces = new("(\\B[A-Z])", RegexOptions.Compiled);
    
    protected string GetFriendlyString(T enumType)
    {
        return _camelCaseAddSpaces.Replace(enumType.ToString(), " $1");
    }
}