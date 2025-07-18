using System;

namespace Fig.Client.Attributes;

/// <summary>
/// IndentAttribute is used to specify the visual indentation level of a setting card in the UI.
/// The indent value multiplies with a base indentation to provide visual hierarchy.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IndentAttribute : Attribute
{
    /// <summary>
    /// Creates an indent attribute with the specified level.
    /// </summary>
    /// <param name="level">The indentation level (0-5 inclusive). Defaults to 1 if not specified.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when level is not between 0 and 5 inclusive.</exception>
    private const int MinIndentLevel = 0;
    private const int MaxIndentLevel = 5;

    public IndentAttribute(int level = 1)
    {
        if (level < MinIndentLevel || level > MaxIndentLevel)
            throw new ArgumentOutOfRangeException(nameof(level), level, $"Indent level must be between {MinIndentLevel} and {MaxIndentLevel} inclusive.");
        
        Level = level;
    }
    
    /// <summary>
    /// Gets the indentation level for this setting.
    /// </summary>
    public int Level { get; }
}
