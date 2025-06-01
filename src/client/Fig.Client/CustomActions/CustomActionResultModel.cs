using System.Collections.Generic;

namespace Fig.Client.CustomActions
{
    /// <summary>
    /// Model representing the result of a custom action execution.
    /// </summary>
    public class CustomActionResultModel
    {
        /// <summary>
        /// Initializes a new instance of the CustomActionResultModel class.
        /// </summary>
        /// <param name="name">The name of the result</param>
        public CustomActionResultModel(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the result.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// A text-based result, if applicable.
        /// </summary>
        public string? TextResult { get; set; }
        
        /// <summary>
        /// A data grid result containing rows and columns of data, if applicable.
        /// </summary>
        public List<Dictionary<string, object?>>? DataGridResult { get; set; }
    }
}
