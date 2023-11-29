using System.Collections.Generic;

namespace Fig.Client.Description;

internal interface IInternalResourceProvider
{ 
    string GetStringResource(string resourceKey);

    List<string> GetAllResourceKeys();
}