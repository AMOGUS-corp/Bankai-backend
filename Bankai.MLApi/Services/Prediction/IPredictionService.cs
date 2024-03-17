using Bankai.MLApi.Data.Entities;

namespace Bankai.MLApi.Services.Prediction;

public interface IPredictionService
{
    Result<IEnumerable<string>> Predict(Model model, string[][] inputData);
}
