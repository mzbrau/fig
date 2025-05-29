using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client.CustomActions
{
    public interface ICustomAction
    {
        string Name { get; }
        string ButtonName { get; }
        string Description { get; }
        IEnumerable<SettingDefinitionDataContract> SettingsUsed { get; }
        Task<IEnumerable<CustomActionResultDataContract>> Execute(IEnumerable<SettingDataContract>? settings, CancellationToken cancellationToken);
    }
}
