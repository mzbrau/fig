using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IDeferredClientConverter
{
    DeferredClientImportBusinessEntity Convert(SettingClientValueExportDataContract client);
    
    SettingClientValueExportDataContract Convert(DeferredClientImportBusinessEntity client);
}