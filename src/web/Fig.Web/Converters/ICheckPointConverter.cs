using Fig.Contracts.CheckPoint;
using Fig.Web.Models.TimeMachine;

namespace Fig.Web.Converters;

public interface ICheckPointConverter
{
    CheckPointModel Convert(CheckPointDataContract dataContract);   
}