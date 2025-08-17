using System;

namespace Fig.Client.Abstractions.Validation;

public interface IValidatableAttribute
{
    Type[]? ApplyToTypes { get; }

    (bool, string) IsValid(object value);
}