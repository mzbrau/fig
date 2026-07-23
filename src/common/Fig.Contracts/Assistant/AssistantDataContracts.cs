using System.Collections.Generic;

namespace Fig.Contracts.Assistant
{
    public class AssistantChatRequestDataContract
    {
        public List<AssistantChatMessageDataContract> Messages { get; set; } = new List<AssistantChatMessageDataContract>();
        public AssistantUiContextDataContract? UiContext { get; set; }
    }

    public class AssistantChatMessageDataContract
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AssistantUiContextDataContract
    {
        public string? CurrentPage { get; set; }
        public string? Route { get; set; }
        public string? SelectedClientName { get; set; }
        public string? SelectedInstance { get; set; }
        public string? SelectedSettingName { get; set; }
        public string? SelectedGroupName { get; set; }
        public string? SelectedLookupTableName { get; set; }
        public string? Username { get; set; }
        public List<AssistantDirtySettingDataContract> DirtySettings { get; set; } = new List<AssistantDirtySettingDataContract>();
    }

    public class AssistantDirtySettingDataContract
    {
        public string Name { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? Instance { get; set; }
        public string? ValueType { get; set; }
        public object? Value { get; set; }
    }

    public static class AssistantStreamEventTypes
    {
        public const string Progress = "progress";
        public const string Token = "token";
        public const string Action = "action";
        public const string Done = "done";
        public const string Error = "error";
    }

    public class AssistantStreamEventDataContract
    {
        public AssistantStreamEventDataContract(string type, object data)
        {
            Type = type;
            Data = data;
        }

        public string Type { get; set; }
        public object Data { get; set; }
    }

    public static class AssistantProposedActionTypes
    {
        public const string UpdateSetting = "updateSetting";
        public const string CreateGroup = "createGroup";
        public const string CreateLookupTable = "createLookupTable";
        public const string CreateInstance = "createInstance";
        public const string SearchSettings = "searchSettings";
        public const string HighlightSetting = "highlightSetting";
        public const string GenerateReport = "generateReport";
    }

    public class AssistantProposedActionDataContract
    {
        public string Type { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? Instance { get; set; }
        public string? SettingName { get; set; }
        public object? Value { get; set; }
        public string? GroupName { get; set; }
        public string? LookupTableName { get; set; }
        public string? Description { get; set; }
        public object? Data { get; set; }
        public string? SearchQuery { get; set; }
        public string? ReportId { get; set; }
        public Dictionary<string, object?>? Parameters { get; set; }
    }

    public class AssistantStatusDataContract
    {
        public bool Enabled { get; set; }
    }
}
