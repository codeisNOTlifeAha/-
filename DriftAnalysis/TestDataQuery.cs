namespace DriftAnalysis;

public static class TestDataQuery
{
    public static string Sql { get; } =
        """
        SELECT 
            t.part_number,
            t.fixture_id,
            t.pfs_code,
            t.operation_name,
            t.[value],
            t.operation_lower_limit,
            t.operation_upper_limit,
            t.test_end_time,
            t.set_temp,
            t.channel,
            t.operation_pass_fail
        FROM dbo.bet_pressure_fct_test_operation t
        WHERE 
            t.part_number = @PartNumber
            AND t.test_end_time >= @StartDate 
            AND t.test_end_time < DATEADD(day, 1, @EndDate)
            AND t.production_data = 1
            AND (CAST(@OperationName AS NVARCHAR(MAX)) = '' OR t.operation_name = @OperationName)
            AND (CAST(@SetTemp AS NVARCHAR(MAX)) = '' OR t.set_temp = CAST(@SetTemp AS FLOAT))
        ORDER BY 
            t.test_end_time ASC;
        """;
}
