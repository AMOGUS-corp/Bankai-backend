namespace Bankai.MLApi.Data.Enums;

/// <summary>
/// 
/// </summary>
public enum ModelAlgorithm
{
    SdcaRegression = 0,
    LbfgsPoissonRegression,
    OnlineGradientDescent,
    LightGbmRegression,
    FastTreeRegression,
    FastForestRegression,
    GamRegression,
    Ols,
    AveragedPerceptron,
    SdcaLogisticRegressionBinary,
    SdcaNonCalibratedBinary,
    LbfgsLogisticRegressionBinary,
    SymbolicSgdLogisticRegressionBinary,
    LightGbmBinary,
    FastTreeBinary,
    FastForestBinary,
    GamBinary,
    FieldAwareFactorizationMachine,
    LinearSvm,
    LdSvm,
    SdcaLogisticRegressionOva,
    SdcaMaximumEntropyMulticlass,
    SdcaMaximumEntropyMulti,
    SdcaNonCalibratedMulticlass,
    SdcaNonCalibratedMulti,
    LbfgsMaximumEntropyMulticlass,
    LbfgsMaximumEntropyMulti,
    LightGbmMulticlass,
    LightGbmMulti,
    LbfgsLogisticRegressionOva,
    FastForestOva,
    FastTreeOva,
    KMeans
}