using System.Globalization;

namespace DriftAnalysis;

public sealed class DriftDetector
{
    private const int WindowSize = 3;
    private const double DriftThresholdRatio = 0.2;
    private const double StabilityLimit = 0.5;

    public IReadOnlyList<DriftDetectionResult> Analyze(IEnumerable<TestOperationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var results = new List<DriftDetectionResult>();

        var grouped = records
            .GroupBy(r => new { r.FixtureId, r.SetTemp, r.Channel, r.OperationName });

        foreach (var group in grouped)
        {
            var cleaned = group
                .Where(r => r.Value >= r.OperationLowerLimit && r.Value <= r.OperationUpperLimit)
                .OrderBy(r => r.TestEndTime)
                .ToList();

            if (cleaned.Count == 0)
            {
                continue;
            }

            var specWidth = cleaned[0].OperationUpperLimit - cleaned[0].OperationLowerLimit;
            if (specWidth <= 0)
            {
                continue;
            }

            var dailyMeans = cleaned
                .GroupBy(r => r.TestEndTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DataPoint(g.Key, g.Average(x => x.Value)))
                .ToList();

            for (var i = WindowSize; i < dailyMeans.Count - WindowSize; i++)
            {
                var previousMean = dailyMeans.Skip(i - WindowSize).Take(WindowSize).Average(d => d.MeanValue);
                var nextMean = dailyMeans.Skip(i).Take(WindowSize).Average(d => d.MeanValue);
                var difference = Math.Abs(nextMean - previousMean);
                var threshold = specWidth * DriftThresholdRatio;

                if (difference < threshold)
                {
                    continue;
                }

                var followUp = dailyMeans.Skip(i + 1).Take(3).Select(d => d.MeanValue).ToList();
                var nature = followUp.Count < 3 || StandardDeviation(followUp) >= StabilityLimit
                    ? DriftNature.OccasionalAnomaly
                    : DriftNature.TrendShift;

                results.Add(new DriftDetectionResult(
                    group.Key.FixtureId,
                    group.Key.SetTemp,
                    group.Key.Channel,
                    group.Key.OperationName,
                    dailyMeans[i].Date,
                    previousMean,
                    nextMean,
                    difference,
                    specWidth,
                    nature));
            }
        }

        return results
            .OrderBy(r => r.DetectedDate)
            .ToList();
    }

    public string BuildAnomalyReport(IEnumerable<DriftDetectionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var anomalies = results.ToList();
        if (anomalies.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var result in anomalies)
        {
            var percentage = result.SpecWidth == 0
                ? 0
                : (result.Difference / result.SpecWidth) * 100;

            lines.Add($"测试项：[{result.OperationName}]");
            lines.Add("•状态： 异常");
            lines.Add("•异常详情：");
            lines.Add($"o在 [{result.DetectedDate:yyyy-MM-dd}] 检测到均值大幅变化。");
            lines.Add($"o前均值：[{result.PreviousMean.ToString("0.###", CultureInfo.InvariantCulture)}]，后均值：[{result.NextMean.ToString("0.###", CultureInfo.InvariantCulture)}]。");
            lines.Add($"o变化幅度：[{result.Difference.ToString("0.###", CultureInfo.InvariantCulture)}] (规格宽度的 [{percentage.ToString("0.##", CultureInfo.InvariantCulture)}]%)。");
            lines.Add($"o类型：[{(result.DriftNature == DriftNature.TrendShift ? "趋势性漂移" : "偶然异常")}]。");
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines).TrimEnd();
    }

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values.Average(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(variance);
    }
}
