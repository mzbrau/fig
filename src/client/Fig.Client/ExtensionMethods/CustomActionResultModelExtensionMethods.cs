using Fig.Client.Abstractions.CustomActions;
using Fig.Client.CustomActions;
using Fig.Contracts.CustomActions;

namespace Fig.Client.ExtensionMethods
{
    /// <summary>
    /// Extension methods for CustomActionResultModel.
    /// </summary>
    internal static class CustomActionResultModelExtensionMethods
    {
        /// <summary>
        /// Converts a CustomActionResultModel to a CustomActionResultDataContract.
        /// </summary>
        /// <param name="model">The model to convert</param>
        /// <returns>The corresponding data contract</returns>
        public static CustomActionResultDataContract ToDataContract(this CustomActionResultModel model)
        {
            return new CustomActionResultDataContract(model.Name, model.Succeeded)
            {
                TextResult = model.TextResult,
                DataGridResult = model.DataGridResult
            };
        }
    }
}
