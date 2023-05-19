using System.Diagnostics;

namespace Fig.Web.Models.WebHooks;

public class WebHookClientModel
{
    private string? _originalName;
    private Uri? _originalBaseUri;
    private string? _uriStr;

    public Guid? Id { get; set; }
    
    public string? Name { get; set; }
    
    public Uri? BaseUri { get; set; }
    
    public string? HashedSecret { get; set; }
    
    public string? Secret { get; set; }

    public string UriStr
    {
        get => _uriStr ?? BaseUri?.ToString() ?? string.Empty;
        set
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var outUri)
                && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps))
            {
                BaseUri = new Uri(value);
            }

            _uriStr = value;
        }
    }

    public void Snapshot()
    {
        _originalName = Name;
        _originalBaseUri = BaseUri != null ? new Uri(BaseUri.AbsoluteUri) : null;
    }

    public string? Validate(List<WebHookClientModel> models)
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Name was not valid";

        if (string.IsNullOrWhiteSpace(BaseUri?.ToString()))
            return "Uri was not valid";

        if (string.IsNullOrEmpty(HashedSecret) && string.IsNullOrEmpty(Secret))
            return "A secret must be specified";

        if (models.Count(a => a.Name == Name) > 1)
            return "Must have a unique name";

        return null;
    }

    public void Revert()
    {
        Name = _originalName;
        BaseUri = _originalBaseUri != null ? new Uri(_originalBaseUri.AbsoluteUri) : null;
    }

    public void HashSecret()
    {
        if (!string.IsNullOrWhiteSpace(Secret))
            HashedSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(Secret);

        Secret = null;
    }
}