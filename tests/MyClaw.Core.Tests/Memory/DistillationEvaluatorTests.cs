using MyClaw.Core.Memory;

namespace MyClaw.Core.Tests.Memory;

public class DistillationEvaluatorTests
{
    private readonly DistillationEvaluator _evaluator = new(tokenBudget: 8000);

    [Fact]
    public void Evaluate_EntryCountAboveThreshold_ReturnsHighUrgency()
    {
        var status = new MemoryStatus { EntryCount = 25 };

        var result = _evaluator.Evaluate(status);

        Assert.True(result.ShouldDistill);
        Assert.Equal(DistillationUrgency.High, result.Urgency);
        Assert.Contains("25 条目", result.Reason);
    }

    [Fact]
    public void Evaluate_BudgetPressureHigh_ReturnsHighUrgency()
    {
        // 8000 token budget, 40% = 3200 tokens, at 4 chars/token = 12800 chars
        var status = new MemoryStatus { LogBytes = 15000 };

        var result = _evaluator.Evaluate(status);

        Assert.True(result.ShouldDistill);
        Assert.Equal(DistillationUrgency.High, result.Urgency);
        Assert.Contains("预算", result.Reason);
    }

    [Fact]
    public void Evaluate_OldestEntryOld_ReturnsMediumUrgency()
    {
        var status = new MemoryStatus
        {
            EntryCount = 8,
            OldestEntryAgeHours = 10
        };

        var result = _evaluator.Evaluate(status);

        Assert.True(result.ShouldDistill);
        Assert.Equal(DistillationUrgency.Medium, result.Urgency);
        Assert.Contains("10", result.Reason);
    }

    [Fact]
    public void Evaluate_OldestEntryOldButFewEntries_ReturnsLowUrgency()
    {
        // Old entry but <= 5 entries
        var status = new MemoryStatus
        {
            EntryCount = 3,
            OldestEntryAgeHours = 10
        };

        var result = _evaluator.Evaluate(status);

        // Should not trigger age-based distillation
        Assert.False(result.ShouldDistill);
    }

    [Fact]
    public void Evaluate_LogSizeLarge_ReturnsLowUrgency()
    {
        var status = new MemoryStatus { LogBytes = 10000 };

        var result = _evaluator.Evaluate(status);

        Assert.True(result.ShouldDistill);
        Assert.Equal(DistillationUrgency.Low, result.Urgency);
        Assert.Contains("10000B", result.Reason);
    }

    [Fact]
    public void Evaluate_NoConditionsMet_ReturnsNoDistill()
    {
        var status = new MemoryStatus
        {
            EntryCount = 3,
            LogBytes = 1000,
            OldestEntryAgeHours = 2
        };

        var result = _evaluator.Evaluate(status);

        Assert.False(result.ShouldDistill);
        Assert.Equal(DistillationUrgency.Low, result.Urgency);
        Assert.Equal("正常", result.Reason);
    }

    [Theory]
    [InlineData(21, 1000, 1, true, DistillationUrgency.High)]   // entries > 20
    [InlineData(5, 15000, 1, true, DistillationUrgency.High)]   // budget > 40%
    [InlineData(8, 1000, 10, true, DistillationUrgency.Medium)] // age > 8h, entries > 5
    [InlineData(3, 10000, 1, true, DistillationUrgency.Low)]    // size > 8KB
    [InlineData(3, 1000, 2, false, DistillationUrgency.Low)]    // none
    public void Evaluate_VariousConditions_ReturnsExpectedResults(
        int entryCount, int logBytes, double ageHours,
        bool shouldDistill, DistillationUrgency urgency)
    {
        var status = new MemoryStatus
        {
            EntryCount = entryCount,
            LogBytes = logBytes,
            OldestEntryAgeHours = ageHours
        };

        var result = _evaluator.Evaluate(status);

        Assert.Equal(shouldDistill, result.ShouldDistill);
        Assert.Equal(urgency, result.Urgency);
    }

    [Fact]
    public void DistillationEvaluation_DefaultConstructor_SetsDefaults()
    {
        var eval = new DistillationEvaluation();

        Assert.False(eval.ShouldDistill);
        Assert.Equal(string.Empty, eval.Reason);
        Assert.Equal(DistillationUrgency.Low, eval.Urgency);
    }

    [Fact]
    public void MemoryStatus_DefaultConstructor_SetsDefaults()
    {
        var status = new MemoryStatus();

        Assert.Equal(0, status.EntryCount);
        Assert.Equal(0, status.LogBytes);
        Assert.Equal(0, status.OldestEntryAgeHours);
    }
}
