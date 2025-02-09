namespace Fig.Contracts.ImportExport
{
    public enum ImportType
    {
        // Clears the database and imports
        ClearAndImport,
        
        // Imports all and replaces any existing clients and settings
        ReplaceExisting,
        
        // Imports but only adds clients that are missing. Others are not imported
        AddNew,
        
        // For value only imports, will update the values with the import values
        UpdateValues,
        
        // For value only imports, will update the values but only if the client has not already registered.
        UpdateValuesInitOnly
    }
}