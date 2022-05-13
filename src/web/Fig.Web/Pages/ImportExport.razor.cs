using System.Data;
using System.Text;
using Fig.Contracts.ImportExport;
using Fig.Web.Facades;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Pages
{
    public partial class ImportExport
    {
        private bool _decryptSecrets;
        private bool _importInProgress;
        private string? _importStatus;
        private bool _importIsInvalid;
        private FigDataExportDataContract? _dataToImport;
        private ImportType _importType;
        private List<ImportType> _settingsImportTypes => Enum.GetValues<ImportType>().ToList();

        [Inject]
        public IDataFacade DataFacade { get; set; }

        [Inject]
        public IJSRuntime JavascriptRuntime { get; set; }

        private async Task PerformSettingsImport()
        {
            if (_dataToImport is null)
                return;

            _dataToImport.ImportType = _importType;
            UpdateStatus("Starting import...");
            var result = await DataFacade.ImportSettings(_dataToImport);

            if (result is not null)
            {
                UpdateStatus("Import Completed Successfully");
                UpdateStatus($"Import Type: {result.ImportType}");
                UpdateStatus($"{result.DeletedClientCount} clients removed.");
                UpdateStatus($"{result.ImportedClientCount} clients added.");
                UpdateStatus($"Added the following:{Environment.NewLine}{string.Join(Environment.NewLine, result.ImportedClients)}");
            }
            else
            {
                UpdateStatus("Import Failed");
            }
        }

        private async Task PerformSettingsExport()
        {
            var data = await DataFacade.ExportSettings(_decryptSecrets);
            var text = JsonConvert.SerializeObject(data);
            await DownloadExport(text);
        }

        private void SettingsImportFileChanged(string args)
        {
            _importIsInvalid = true;
            try
            {
                var trimmed = args.Substring(args.IndexOf(',') + 1);
                byte[] data = Convert.FromBase64String(trimmed);
                string decodedString = Encoding.UTF8.GetString(data);
                _dataToImport = JsonConvert.DeserializeObject<FigDataExportDataContract>(decodedString);

                if (_dataToImport is null)
                    throw new DataException("Invalid input data");

                _importType = _dataToImport.ImportType;
                UpdateStatus($"File was export at {_dataToImport.ExportedAt.ToLocalTime()} with version {_dataToImport.Version}");
                UpdateStatus($"Import contains {_dataToImport.Clients.Count} client(s).");
                foreach (var client in _dataToImport.Clients)
                {
                    UpdateStatus($"{client.Name} -> {client.Settings.Count} settings, {client.DynamicVerifications.Count} dynamic verifications, {client.PluginVerifications.Count} plugin verifications");
                }

                UpdateStatus("Ready for import");
                _importIsInvalid = false;
            }
            catch (Exception e)
            {
                UpdateStatus($"Invalid Json Export file. {e.Message}");
            }
            
            _importInProgress = true;
        }

        private void OnImportFileError(UploadErrorEventArgs args)
        {
            UpdateStatus(args.Message);
        }

        private void UpdateStatus(string text)
        {
            _importStatus += text + Environment.NewLine;
        }

        private async Task DownloadExport(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            await FileUtil.SaveAs(JavascriptRuntime, $"FigExport-{DateTime.Now:s}.json", bytes);
        }
    }
}
