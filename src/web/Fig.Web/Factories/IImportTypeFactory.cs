using Fig.Web.Models.ImportExport;

namespace Fig.Web.Factories;

public interface IImportTypeFactory
{
    IEnumerable<ImportTypeEnumerable> GetImportTypes();
}