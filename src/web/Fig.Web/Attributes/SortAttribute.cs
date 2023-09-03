namespace Fig.Web.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SortAttribute : Attribute
{
    public bool Ascending { get; }

    public SortAttribute(bool ascending = true)
    {
        Ascending = ascending;
    }
}