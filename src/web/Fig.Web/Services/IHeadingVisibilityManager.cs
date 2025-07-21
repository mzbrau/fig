using Fig.Web.Models;
using Fig.Web.Models.Setting;

namespace Fig.Web.Services;

public interface IHeadingVisibilityManager
{
    void RegisterHeading(string headingId, List<ISetting> referencedSettings, HeadingType headingType);
    
    void UnregisterHeading(string headingId);
    
    bool IsVisibleForHeading(string headingId);
    
    event EventHandler<string> HeadingVisibilityChanged;
}