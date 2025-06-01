using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionResultDataContract
    {
        public CustomActionResultDataContract(string name, bool succeeded)
        {
            Name = name;
            Succeeded = succeeded;
        }

        public string Name { get; set; }
        
        public string? TextResult { get; set; }
        
        public bool Succeeded { get; set; }
        
        public List<Dictionary<string, object?>>? DataGridResult { get; set; }
    }
}
