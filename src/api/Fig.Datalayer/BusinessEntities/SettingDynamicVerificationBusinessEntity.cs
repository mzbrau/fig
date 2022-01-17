using Fig.Contracts.SettingVerification;

namespace Fig.Datalayer.BusinessEntities;

public class SettingDynamicVerificationBusinessEntity : SettingVerificationBase
{
    public virtual string Description { get; set; }
        
    public virtual string Code { get; set; }

    public virtual TargetRuntime TargetRuntime { get; set; }
    
    public virtual string Checksum { get; set; }
}