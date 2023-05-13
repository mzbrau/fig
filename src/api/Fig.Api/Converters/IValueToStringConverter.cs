namespace Fig.Api.Converters;

public interface IValueToStringConverter
{
    string Convert(object? value);
}