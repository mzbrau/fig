namespace Fig.Client.Attributes;

public interface IDisplayScriptProvider
{
    string GetScript(string propertyName);
}