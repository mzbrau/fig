using System;

namespace Fig.Client.Abstractions.StatusProperties
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StatusPropertyAttribute : Attribute
    {
        public string? DisplayName { get; set; }

        /// <summary>
        /// Include in the Connected Clients collapsed column.
        /// </summary>
        public bool Highlight { get; set; }

        /// <summary>
        /// When false, omit from Fig.Web; still available via REST/MCP.
        /// </summary>
        public bool ShowInUi { get; set; } = true;

        /// <summary>
        /// Sort order within collapsed and expanded UI (ascending).
        /// </summary>
        public int Order { get; set; }
    }
}
