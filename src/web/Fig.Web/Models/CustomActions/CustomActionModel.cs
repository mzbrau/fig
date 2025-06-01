using Fig.Contracts.CustomActions;

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionModel
    {
        private readonly CustomActionDefinitionDataContract _dataContract;
        
        public CustomActionModel(CustomActionDefinitionDataContract dataContract)
        {
            _dataContract = dataContract;
        }
        
        public string Name => _dataContract.Name;
        public string ButtonName => _dataContract.ButtonName;
        public string Description => _dataContract.Description;
        public string SettingsUsed => _dataContract.SettingsUsed;

        public bool IsCompactView { get; set; }
        public bool IsHistoryVisible { get; set; }
        
        public CustomActionExecutionStatusDataContract? ExecutionStatus { get; set; }
        
        public void ToggleCompactView()
        {
            IsCompactView = !IsCompactView;
        }
        
        public void ShowHistory()
        {
            IsHistoryVisible = !IsHistoryVisible;
        }
    }
}
