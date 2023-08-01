using System.Reflection;

namespace Fig.Common.MarkdownUtils.Description;

public interface IInternalResourceProvider
{ 
    string GetStringResource(string resourceKey, Assembly? assemblyWithResource = null);
}