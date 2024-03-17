using AutoMapper;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Models.Dtos;

namespace Bankai.MLApi.Utils;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<Model, ModelInformation>();
        CreateMap<Model, ModelDetailedInformation>();
    }
}
