using Fig.Contracts.ImportExport;
using Fig.Web.Models.ImportExport;

namespace Fig.Web.Factories;

public class ImportTypeFactory : EnumFriendlyNameBase<ImportType>, IImportTypeFactory
{
    public IEnumerable<ImportTypeEnumerable> GetImportTypes()
    {
        foreach (var item in Enum.GetValues(typeof(ImportType)))
        {
            yield return new ImportTypeEnumerable((ImportType)item, GetFriendlyString((ImportType)item));
        }
    }
}