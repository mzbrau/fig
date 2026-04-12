using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;

namespace Fig.Api.Services;

public interface IGroupImportExportService : IAuthenticatedService
{
    Task<SettingGroupExportDataContract> ExportGroups();
    
    Task<ImportResultDataContract> ImportGroups(SettingGroupExportDataContract data, ImportType importType);
}
