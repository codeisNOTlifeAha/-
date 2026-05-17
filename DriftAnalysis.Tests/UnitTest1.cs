namespace DriftAnalysis.Tests;

public class UnitTest1
{
    private readonly DriftDetector _detector = new();

    [Fact]
    public void Analyze_ShouldFilterOutOfSpecData_BeforeDailyAggregation()
    {
        var start = new DateTime(2026, 1, 1);
        var values = new[] { 5d, 5d, 5d, 50d, 5d, 5d, 5d };

        var records = values
            .Select((v, idx) => CreateRecord(v, start.AddDays(idx), "LeakTest"))
            .ToList();

        var result = _detector.Analyze(records);

        Assert.Empty(result);
    }

    [Fact]
    public void Analyze_ShouldClassifyTrendShift_WhenFollowUpIsStable()
    {
        var start = new DateTime(2026, 1, 1);
        var values = new[] { 1d, 1d, 1d, 1d, 8d, 8d, 8d, 8d, 8d, 8d };

        var records = values
            .Select((v, idx) => CreateRecord(v, start.AddDays(idx), "PressureRise"))
            .ToList();

        var result = _detector.Analyze(records);

        Assert.Contains(result, r => r.DriftNature == DriftNature.TrendShift && r.DetectedDate == start.AddDays(3));
    }

    [Fact]
    public void Analyze_ShouldClassifyOccasionalAnomaly_WhenFollowUpFluctuates()
    {
        var start = new DateTime(2026, 1, 1);
        var values = new[] { 1d, 1d, 1d, 1d, 8d, 5d, 9d, 8d };

        var records = values
            .Select((v, idx) => CreateRecord(v, start.AddDays(idx), "PressureDrop"))
            .ToList();

        var result = _detector.Analyze(records);

        Assert.All(result, r => Assert.Equal(DriftNature.OccasionalAnomaly, r.DriftNature));
        Assert.Contains(result, r => r.DetectedDate == start.AddDays(3));
    }

    [Fact]
    public void BuildAnomalyReport_ShouldReturnOnlyAnomalies()
    {
        var report = _detector.BuildAnomalyReport(Array.Empty<DriftDetectionResult>());

        Assert.Equal(string.Empty, report);
    }

    private static TestOperationRecord CreateRecord(double value, DateTime time, string operationName)
        => new(
            PartNumber: "0051027300001",
            FixtureId: "FX-01",
            PfsCode: "PFS",
            OperationName: operationName,
            Value: value,
            OperationLowerLimit: 0,
            OperationUpperLimit: 10,
            TestEndTime: time,
            SetTemp: 25,
            Channel: "CH1",
            OperationPassFail: "P");
}
