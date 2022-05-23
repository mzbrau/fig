using Fig.Contracts.Configuration;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IFigConfigurationConverter
{
    FigConfigurationDataContract Convert(FigConfigurationBusinessEntity configuration);
}