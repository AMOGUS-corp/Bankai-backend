using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Infrastructure;
using Bankai.MLApi.Infrastructure.Extensions;
using Microsoft.ML.Data;
using static Bankai.MLApi.Data.Enums.PredictionType;

namespace Bankai.MLApi.Services.Prediction;

public class PredictionService : IPredictionService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(MLContext mlContext, ILogger<PredictionService> logger)
    {
        _mlContext = mlContext;
        _logger = logger;
    }

    public Result<IEnumerable<string>> Predict(Model model, string[][] inputData) =>
        Result.SuccessIf(InputDataIsValid(model, inputData), (model, inputData), "Incorrect input data")
            .BindTry(t => InputTypeFactory.Create(t.model, t.inputData, null, _mlContext))
            .TapError(e => _logger.LogError("Input type creating error: {Exception}", e))
            .Tap(_ => _logger.LogInformation("Try to predict ({Id})", model.Id))
            .MapTry(dv => _mlContext.Model
                .Load(model.Data?.ToStream(), out _)
                .Transform(dv))
            .Map(data => model.Prediction switch
            {
                Regression => data.GetColumn<float>("Score").Select(x => x.ToString(InvariantCulture)),
                BinaryClassification => data.GetColumn<float>("Probability").Select(x => x.ToString(InvariantCulture)),
                MulticlassClassification => data.GetColumn<string>("PredictedLabel").Select(x => x.ToString(InvariantCulture)),
                _ => data.GetColumn<uint>("PredictedLabel").Select(x => x.ToString())
            })
            .Ensure(predict => predict.Any(), "Prediction error")
            .TapError(e => _logger.LogError("Error while predict: {Exception}", e));

    private static bool InputDataIsValid(Model model, IEnumerable<IEnumerable<string>> inputData) =>
        model.Features.Where(f => !f.IsTarget).Zip(inputData,
            (f, d) => f.Type switch
            {
                var val when typeof(float).ToString().Contains(val) => float.TryParse(d.First(), NumberStyles.Float,
                    InvariantCulture, out _),
                var val when typeof(bool).ToString().Contains(val) => bool.TryParse(d.First(), out _), _ => true
            }).All(result => result);
}
