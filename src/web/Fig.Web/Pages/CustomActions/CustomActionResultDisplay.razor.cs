using Fig.Web.Models.CustomActions;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages.CustomActions
{
    public partial class CustomActionResultDisplay
    {
        [Parameter]
        public CustomActionResultModel Result { get; set; } = null!;
    }
}
