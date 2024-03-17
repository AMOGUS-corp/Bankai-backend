using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Models.Dtos;

public class ModelStatusInformation
{
    public Guid Id { get; set; }
    
    public ModelState State { get; set; } = ModelState.Empty;

    public string Status { get; set; } = String.Empty;
}