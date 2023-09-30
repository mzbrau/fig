using Fig.Contracts.ImportExport;

namespace Fig.Web.Models.ImportExport;

public class ImportTypeEnumerable
{
    public ImportTypeEnumerable(ImportType enumValue, string enumName)
    {
        EnumValue = enumValue;
        EnumName = enumName;
    }

    public ImportType EnumValue { get; }
        
    public string EnumName { get; }
}