using MyClaw.Core.Evolution;

namespace MyClaw.Core.Tests.Evolution;

public class SignalDetectorTests
{
    private readonly SignalDetector _detector = new();

    #region User Preference Tests

    [Theory]
    [InlineData("我喜欢使用VS Code")]
    [InlineData("I like the dark theme")]
    [InlineData("don't use TypeScript")]
    [InlineData("以后请使用中文回答")]
    public void DetectSignals_UserPreferenceIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.UserPreference);
    }

    [Theory]
    [InlineData("How do I configure this?")]
    [InlineData("What's the weather today")]
    [InlineData("Tell me a joke")]
    public void DetectSignals_NoPreferenceIndicators_ShouldNotReturnUserPreference(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.DoesNotContain(signals, s => s.SignalType == EvolutionSignal.UserPreference);
    }

    #endregion

    #region Personality Correction Tests

    [Theory]
    [InlineData("别那么严肃")]
    [InlineData("more lively")]
    [InlineData("你是一个")]
    [InlineData("change personality")]
    public void DetectSignals_PersonalityIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.PersonalityCorrection);
    }

    #endregion

    #region Environment Config Tests

    [Theory]
    [InlineData("项目用的是")]
    [InlineData("server IP")]
    [InlineData("API key")]
    public void DetectSignals_ConfigIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.EnvironmentConfig);
    }

    #endregion

    #region Tool Experience Tests

    [Theory]
    [InlineData("这个工具的参数")]
    [InlineData("踩坑记录")]
    public void DetectSignals_ToolExperienceIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.ToolExperience);
    }

    #endregion

    #region Identity Change Tests

    [Theory]
    [InlineData("叫你自己Claw")]
    [InlineData("your name is")]
    [InlineData("改名")]
    public void DetectSignals_IdentityIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.IdentityChange);
    }

    #endregion

    #region Workflow Learned Tests

    [Theory]
    [InlineData("最好的实践是")]
    [InlineData("以后都按这个流程")]
    [InlineData("标准化")]
    public void DetectSignals_WorkflowIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.WorkflowLearned);
    }

    #endregion

    #region Important Fact Tests

    [Theory]
    [InlineData("重要")]
    [InlineData("记住这个")]
    [InlineData("别忘了")]
    [InlineData("mark this")]
    public void DetectSignals_ImportantFactIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.ImportantFact);
    }

    #endregion

    #region Distinct Signals Tests

    [Fact]
    public void DetectSignals_WithMultipleMatchesSameType_ShouldReturnDistinctSignals()
    {
        // "我喜欢" 和 "别那么严肃" 是不同信号类型
        var input = "我喜欢用Python，别那么严肃";
        var signals = _detector.DetectSignals(input);

        // 应该检测到两种不同信号
        var signalTypes = signals.Select(s => s.SignalType).ToList();
        Assert.Contains(EvolutionSignal.UserPreference, signalTypes);
        Assert.Contains(EvolutionSignal.PersonalityCorrection, signalTypes);
    }

    [Fact]
    public void DetectSignals_EmptyInput_ShouldReturnEmptyList()
    {
        var signals = _detector.DetectSignals("");

        Assert.Empty(signals);
    }

    [Fact]
    public void DetectSignals_NullInput_ShouldReturnEmptyList()
    {
        var signals = _detector.DetectSignals(null!);

        Assert.Empty(signals);
    }

    #endregion

    #region Signal Properties Tests

    [Fact]
    public void DetectSignals_ShouldSetCorrectProperties()
    {
        var signals = _detector.DetectSignals("我喜欢用 dark theme");

        var signal = signals.First(s => s.SignalType == EvolutionSignal.UserPreference);
        Assert.Equal("USER.md", signal.TargetFile);
        Assert.Equal("miniclaw_update", signal.SuggestedTool);
        Assert.True(signal.Confidence > 0);
        Assert.NotEmpty(signal.MatchedContent);
    }

    #endregion

    #region GenerateEvolutionAdvice Tests

    [Fact]
    public void GenerateEvolutionAdvice_WithSignals_ShouldReturnAdvice()
    {
        var signals = _detector.DetectSignals("我喜欢用 dark theme");
        var advice = _detector.GenerateEvolutionAdvice(signals);

        Assert.Contains("进化信号", advice);
        Assert.Contains("USER.md", advice);
    }

    [Fact]
    public void GenerateEvolutionAdvice_NoSignals_ShouldReturnEmpty()
    {
        var advice = _detector.GenerateEvolutionAdvice(new List<DetectedSignal>());

        Assert.Empty(advice);
    }

    #endregion
}
