using Fig.Web.Models.Setting;

namespace Fig.Web.Scripting;

public interface IScriptRunner
{
    void RunScript(string? script, SettingClientConfigurationModel client);

    string FormatScript(string script);
}