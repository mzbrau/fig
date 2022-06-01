using Fig.Web.Models.CommonEnumerations;

namespace Fig.Web.Facades;

public interface ICommonEnumerationFacade
{
    List<CommonEnumerationModel> Items { get; }

    Task LoadAll();

    CommonEnumerationModel CreateNew();

    Task Save(CommonEnumerationModel item);
    
    Task Delete(CommonEnumerationModel selectedItem);
}