using System.Text;
using Fig.Web.Exceptions;

namespace Fig.Web.Models.LookupTables;

public class LookupTable
{
    private string? _originalLookupsAsText;

    public LookupTable(Guid? id, string name, List<LookupTableItemModel> lookups, bool isClientDefined)
    {
        Id = id;
        Name = name;
        Lookups = lookups;
        IsClientDefined = isClientDefined;
    }

    public LookupTable(string name, string lookupsAsText)
    {
        Name = name;
        LookupsAsText = lookupsAsText;
    }

    public Guid? Id { get; set; }

    public string Name { get; set; }

    public List<LookupTableItemModel> Lookups { get; set; } = new();

    public bool IsEditing { get; private set; }

    public string? LookupsAsText { get; set; }
    
    public bool IsClientDefined { get; set; }
    
    public void StartEditing()
    {
        if (IsClientDefined)
            return; // Don't allow editing client-defined tables
            
        IsEditing = true;
        var builder = new StringBuilder();
        foreach (var item in Lookups)
        {
            if (string.IsNullOrWhiteSpace(item.Alias))
            {
                builder.AppendLine(item.Key);
            }
            else
            {
                builder.AppendLine($"{item.Key},{item.Alias}");
            }
        }

        LookupsAsText = builder.ToString();
        _originalLookupsAsText = LookupsAsText; // Store original value for revert
    }

    public void CancelEdit()
    {
        IsEditing = false;
        LookupsAsText = _originalLookupsAsText; // Restore original value
    }

    public void Save()
    {
        if (IsClientDefined)
            return; // Don't allow saving client-defined tables
            
        if (LookupsAsText is null)
            return;
        
        ValidateValuesHaveBeenEntered();

        var rows = LookupsAsText.Split('\n').Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

        ValidateThereAreRows();

        var rowTokens = rows.Select(a => a.Split(',')).ToList();

        ValidateThatEachRowHasAKeyAndOptionalAlias();
        ValidateThatKeysAndValuesAreNotEmpty();
        ValidateThereAreNoDuplicateKeys();

        Lookups.Clear();

        foreach (var rowToken in rowTokens)
        {
            Lookups.Add(rowToken.Length == 1
                ? new LookupTableItemModel(rowToken[0].Trim(), null)
                : new LookupTableItemModel(rowToken[0].Trim(), rowToken[1].Trim()));
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

        void ValidateThatEachRowHasAKeyAndOptionalAlias()
        {
            if (rowTokens.Any(a => a.Length > 2))
                throw new InvalidInputException(
                    "At least 1 row contains an incorrect number of commas. Rows may contain no commas or one comma (if alias are used).");
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