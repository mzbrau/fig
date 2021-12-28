namespace Fig.Contracts.Settings
{
    public interface ISetting
    {
        string Name { get; set; }
        
        object Value { get; set; }
    }
}