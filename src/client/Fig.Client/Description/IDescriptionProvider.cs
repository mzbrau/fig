using System.Collections.Generic;

namespace Fig.Client.Description;

internal interface IDescriptionProvider
{
    string GetDescription(string descriptionOrKey);

    List<string> GetAllMarkdownResourceKeys();
}