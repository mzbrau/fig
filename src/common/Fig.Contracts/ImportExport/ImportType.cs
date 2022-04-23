namespace Fig.Contracts.ImportExport
{
    public enum ImportType
    {
        // Clears the database and imports
        ClearAndImport,
        
        // Imports all and replaces any existing clients and settings
        ReplaceExisting,
        
        // Imports but only adds clients that are missing. Others are not imported
        AddNew
    }
}