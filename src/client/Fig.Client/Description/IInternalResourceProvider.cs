using System.Collections.Generic;

namespace Fig.Client.Description;

internal interface IInternalResourceProvider
{ 
    string GetStringResource(string resourceKey);

    byte[]? GetImageResourceBytes(string imagePath);

    List<string> GetAllMarkdownResourceKeys();
}