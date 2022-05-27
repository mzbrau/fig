using Fig.Contracts.Configuration;
using Fig.Web.Models.Configuration;

namespace Fig.Web.Converters;

public interface IFigConfigurationConverter
{
    FigConfigurationDataContract Convert(FigConfigurationModel model);

    FigConfigurationModel Convert(FigConfigurationDataContract dataContract);
}