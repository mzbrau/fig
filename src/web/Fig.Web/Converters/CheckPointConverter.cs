using Fig.Contracts.CheckPoint;
using Fig.Web.Models.TimeMachine;

namespace Fig.Web.Converters;

public class CheckPointConverter : ICheckPointConverter
{
    public CheckPointModel Convert(CheckPointDataContract dataContract)
    {
        return new CheckPointModel(dataContract.Id,
            dataContract.DataId,
            dataContract.Timestamp.ToLocalTime(),
            dataContract.NumberOfClients,
            dataContract.NumberOfSettings,
            dataContract.AfterEvent,
            dataContract.Note,
            dataContract.User);
    }
}