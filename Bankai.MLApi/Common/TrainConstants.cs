namespace Bankai.MLApi.Common;

public static class TrainConstants
{
    public const string TestFraction = nameof(TestFraction);
    public const string NumberOfFolds = nameof(NumberOfFolds);
    public const string TargetMetric = nameof(TargetMetric);
    public const string DefaultRegressionMetric = "RSquared";
    public const string DefaultBinaryClassificationMetric = "Accuracy";
    public const string DefaultMulticlassClassificationMetric = "MacroAccuracy";
    public const string DefaultClusteringMetric = "AverageDistance";
    public const string DefaultTestFraction = "0.2";
    public const string DefaultNumberOfFolds = "2";
    public const string TunerName = nameof(TunerName);
    public const string DefaultTunerName = "EciCostFrugal";
    public const string Pipeline = "_pipeline_";
}