using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;

namespace Fig.Client.CustomActions
{
    public interface ICustomAction
    {
        /// <summary>
        /// The name of the custom action. This should be unique for this client.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// The text that will be displayed in the button in the UI
        /// </summary>
        string ButtonName { get; }
        
        /// <summary>
        /// The accompanying description of the custom action. This will be displayed in the UI.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// The names of the settings that this custom action uses. Just for information purposes.
        /// </summary>
        IEnumerable<string> SettingsUsed { get; }
        
        /// <summary>
        /// Execute the custom action.
        /// </summary>
        /// <returns>A structured set of results</returns>
        Task<IEnumerable<CustomActionResultModel>> Execute(CancellationToken cancellationToken);
    }
}
