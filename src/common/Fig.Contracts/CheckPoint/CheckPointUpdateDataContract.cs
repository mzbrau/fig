namespace Fig.Contracts.CheckPoint;

public class CheckPointUpdateDataContract
{
    public CheckPointUpdateDataContract(string note)
    {
        Note = note;
    }
    
    public string Note { get; }
}