namespace Bankai.MLApi.Data.Enums;

/// <summary>
/// 
/// </summary>
public enum ModelState
{
    Empty = 0,
    Training,
    Ready,
    CalculatingFeatureImportance,
    TrainingFailed,
    CalculatingFeatureImportanceFailed
}