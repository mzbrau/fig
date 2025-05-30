using System.Collections.Generic;
using Fig.Contracts.CustomActions; // For CustomActionResultDataContract

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionResultModel
    {
        public string Name { get; set; }
        public string? TextResult { get; set; }
        public List<Dictionary<string, object?>>? DataGridResult { get; set; }

        public CustomActionResultModel(CustomActionResultDataContract contract)
        {
            Name = contract.Name;
            TextResult = contract.TextResult;
            DataGridResult = contract.DataGridResult;
        }
    }
}
