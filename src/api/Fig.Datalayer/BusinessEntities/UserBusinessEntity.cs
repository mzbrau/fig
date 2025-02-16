using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;
using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class UserBusinessEntity
{
    private string? _allowedClassificationsJson;
    
    public virtual Guid Id { get; init; }
    
    public virtual string Username { get; set; } = default!;
    
    public virtual string FirstName { get; set; } = default!;
    
    public virtual string LastName { get; set; } = default!;
    
    public virtual Role Role { get; set; }

    public virtual string ClientFilter { get; set; } = default!;
    
    public virtual string PasswordHash { get; set; } = default!;

    public virtual List<Classification>? AllowedClassifications { get; set; } =
        Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();
    
    public virtual string? AllowedClassificationsJson
    {
        get
        {
            if (AllowedClassifications == null)
                AllowedClassifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();

            _allowedClassificationsJson = JsonConvert.SerializeObject(AllowedClassifications);
            return _allowedClassificationsJson;
        }
        set
        {
            if (_allowedClassificationsJson != value)
                AllowedClassifications = value != null
                    ? JsonConvert.DeserializeObject<List<Classification>>(value)
                    : Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();
        }
    }
}