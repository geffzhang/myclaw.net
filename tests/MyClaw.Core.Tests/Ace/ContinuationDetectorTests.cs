using MyClaw.Core.Ace;

namespace MyClaw.Core.Tests.Ace;

public class ContinuationDetectorTests
{
    private readonly ContinuationDetector _detector = new();

    [Fact]
    public void Detect_NoLastActivity_ShouldReturnNoReturn()
    {
        var result = _detector.Detect("Some log", null);

        Assert.False(result.IsReturn);
        Assert.Equal(0, result.HoursSinceLastActivity);
    }

    [Fact]
    public void Detect_RecentActivity_ShouldReturnNoReturn()
    {
        var lastActivity = DateTime.Now.AddMinutes(-30); // 30 minutes ago (note: minus!)

        var result = _detector.Detect("Some log", lastActivity);

        Assert.False(result.IsReturn);
    }

    [Fact]
    public void Detect_OldActivity_ShouldReturnIsReturn()
    {
        var lastActivity = DateTime.Now.AddHours(-2); // 2 hours ago (note: minus!)

        var result = _detector.Detect("- [10:00] Started task", lastActivity);

        Assert.True(result.IsReturn);
        Assert.True(result.HoursSinceLastActivity >= 1.9);
    }

    [Fact]
    public void Detect_WithLogEntries_ShouldExtractLastTopic()
    {
        var log = @"- [09:00] Started working
- [10:30] Meeting with team
- [14:00] Implementation work";

        var result = _detector.Detect(log, DateTime.Now.AddHours(-3));

        Assert.True(result.IsReturn);
        Assert.Contains("Implementation work", result.LastTopic);
    }

    [Fact]
    public void Detect_WithDecisions_ShouldExtractDecisions()
    {
        var log = @"- [09:00] Decided to use React
- [10:00] Chosen PostgreSQL for database
- [11:00] Regular update";

        var result = _detector.Detect(log, DateTime.Now.AddHours(-3));

        Assert.True(result.RecentDecisions.Count >= 2);
    }

    [Fact]
    public void Detect_WithOpenQuestions_ShouldExtractQuestions()
    {
        var log = @"- [09:00] How to handle errors?
- [10:00] TODO: add tests
- [11:00] 需要确认需求";

        var result = _detector.Detect(log, DateTime.Now.AddHours(-3));

        Assert.True(result.OpenQuestions.Count >= 3);
    }

    [Fact]
    public void Detect_EmptyLog_ShouldHandleGracefully()
    {
        var result = _detector.Detect("", DateTime.Now.AddHours(-3));

        Assert.True(result.IsReturn);
        Assert.Equal(string.Empty, result.LastTopic);
    }

    [Fact]
    public void ContinuationResult_DefaultValues_ShouldBeSet()
    {
        var result = new ContinuationResult();

        Assert.False(result.IsReturn);
        Assert.Equal(0, result.HoursSinceLastActivity);
        Assert.Equal(string.Empty, result.LastTopic);
        Assert.NotNull(result.RecentDecisions);
        Assert.NotNull(result.OpenQuestions);
    }
}
