namespace DriftAnalysis;

public sealed record TestOperationRecord(
    string PartNumber,
    string FixtureId,
    string PfsCode,
    string OperationName,
    double Value,
    double OperationLowerLimit,
    double OperationUpperLimit,
    DateTime TestEndTime,
    double? SetTemp,
    string Channel,
    string OperationPassFail);

public sealed record DataPoint(DateTime Date, double MeanValue);

public enum DriftNature
{
    TrendShift,
    OccasionalAnomaly,
}

public sealed record DriftDetectionResult(
    string FixtureId,
    double? SetTemp,
    string Channel,
    string OperationName,
    DateTime DetectedDate,
    double PreviousMean,
    double NextMean,
    double Difference,
    double SpecWidth,
    DriftNature DriftNature);
