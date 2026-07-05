
using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class SettingsWithNesting : TestSettingsBase
{
    public override string ClientDescription => "Settings with nesting";
    
    public override string ClientName => "SettingsWithNesting";
    
    [NestedSetting]
    public School? School { get; set; }
    
    [Setting("Subject")]
    public Subject? Subject { get; set; }
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class School
{
    [Setting("Name")]
    public string? Name { get; set; }
    
    [Setting("Students")]
    public List<Student>? Students { get; set; }
    
    [Setting("Subjects", defaultValueMethodName: nameof(GetDefaultSubjects))]
    public List<Subject>? Subjects { get; set; }

    public static List<Subject> GetDefaultSubjects() =>
    [
        new() { Name = "Math", Grade = 90 },
        new() { Name = "English", Grade = 85 }
    ];
}

public class Student
{
    public string? Name { get; set; }
    
    public List<Subject>? Subjects { get; set; }
}

public class Subject
{
    public string? Name { get; set; }
    
    public int? Grade { get; set; }
}