using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionResultDataContract
    {
        public CustomActionResultDataContract(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        
        public string? TextResult { get; set; }
        
        public List<Dictionary<string, object?>>? DataGridResult { get; set; }
    }
}
