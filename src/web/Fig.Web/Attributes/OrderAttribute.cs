namespace Fig.Web.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class OrderAttribute : Attribute
{
    public int Value { get; }

    public OrderAttribute(int value)
    {
        Value = value;
    }
}