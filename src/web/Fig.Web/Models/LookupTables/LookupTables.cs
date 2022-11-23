using System.Text;
using Fig.Web.Exceptions;

namespace Fig.Web.Models.LookupTables;

public class LookupTables
{
    public LookupTables(Guid? id, string name, List<LookupTablesItemModel> lookups)
    {
        Id = id;
        Name = name;
        Lookups = lookups;
    }

    public LookupTables(string name, string lookupsAsText)
    {
        Name = name;
        LookupsAsText = lookupsAsText;
    }

    public Guid? Id { get; set; }

    public string Name { get; set; }

    public List<LookupTablesItemModel> Lookups { get; set; } = new();

    public bool IsEditing { get; set; }

    public string? LookupsAsText { get; set; }
    
    public void StartEditing()
    {
        IsEditing = true;
        var builder = new StringBuilder();
        foreach (var item in Lookups)
        {
            builder.AppendLine($"{item.Key},{item.Value}");
        }

        LookupsAsText = builder.ToString();
    }

    public void Save()
    {
        if (LookupsAsText is null)
            return;
        
        ValidateValuesHaveBeenEntered();

        var rows = LookupsAsText.Split('\n').Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

        ValidateThereAreRows();

        var rowTokens = rows.Select(a => a.Split(',')).ToList();

        ValidateThatEachRowHasOnlyAKeyAndValue();
        ValidateThatKeysAndValuesAreNotEmpty();
        ValidateThereAreNoDuplicateKeys();

        Lookups.Clear();

        foreach (var rowToken in rowTokens)
        {
            Lookups.Add(new LookupTablesItemModel()
            {
                Key = rowToken[0].Trim(),
                Value = rowToken[1].Trim()
            });
        }

        IsEditing = false;

        void ValidateValuesHaveBeenEntered()
        {
            if (string.IsNullOrEmpty(LookupsAsText))
                throw new InvalidInputException("No items were entered");
        }
        
        void ValidateThereAreRows()
        {
            if (!rows.Any())
                throw new InvalidInputException("No items were entered");
        }

        void ValidateThatEachRowHasOnlyAKeyAndValue()
        {
            if (rowTokens.Any(a => a.Length != 2))
                throw new InvalidInputException(
                    "At least 1 row contains an incorrect number of commas. Only 1 comma is permitted per row.");
        }

        void ValidateThatKeysAndValuesAreNotEmpty()
        {
            if (rowTokens.SelectMany(a => a).Any(string.IsNullOrWhiteSpace))
                throw new InvalidInputException(
                    "At least 1 row contains a blank key or value.");
        }

        void ValidateThereAreNoDuplicateKeys()
        {
            var keys = rowTokens.Select(a => a[0].Trim()).ToList();
            var keysHashSet = new HashSet<string>(keys);
            if (keys.Count() != keysHashSet.Count)
                throw new InvalidInputException(
                    "Collection contains at least 1 duplicate key.");
        }
    }
}