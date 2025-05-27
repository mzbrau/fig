using System;

namespace Fig.Client.Validation;

public interface IValidatableAttribute
{
    Type[]? ApplyToTypes { get; }

    (bool, string) IsValid(object value);
}