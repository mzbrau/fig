using System;

namespace Fig.Client.Scripting
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayScriptDefinitionAttribute : Attribute
    {
        public string ScriptTemplate { get; }

        public DisplayScriptDefinitionAttribute(string scriptTemplate)
        {
            ScriptTemplate = scriptTemplate;
        }
    }
}
