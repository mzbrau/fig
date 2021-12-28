using System;

namespace Fig.Client.Attributes
{
    public class GroupAttribute : Attribute
    {
        public GroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
        
        public string GroupName { get; }
    }
}