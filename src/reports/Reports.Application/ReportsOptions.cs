namespace Reports.Application;

public sealed class ReportsOptions
{
    public const string SectionName = "Reports";

    /// <summary>UTC hour (0-23) when the nightly snapshot job runs. Default: 1 (01:00 UTC).</summary>
    public int SnapshotJobHourUtc { get; set; } = 1;
}
