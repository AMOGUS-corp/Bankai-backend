namespace Bankai.MLApi.Models;

public enum AutoMLSetting
{
    Validation = 0,
    Tuner,
    FastTreeSearchSpace,
    LgbmSearchSpace,
    FastForestSearchSpace,
    LbfgsLogisticRegressionSearchSpace,
    LbfgsMaximumEntrophySearchSpace,
    SdcaLogisticRegressionSearchSpace,
    SdcaMaximumEntorphySearchSpace
}