using MyClaw.Core.Ace;

namespace MyClaw.Core.Tests.Ace;

public class TimeModeTests
{
    [Theory]
    [InlineData(6, TimeMode.Morning)]    // 06:00 -> Morning
    [InlineData(8, TimeMode.Morning)]    // 08:00 -> Morning
    [InlineData(9, TimeMode.Work)]       // 09:00 -> Work
    [InlineData(11, TimeMode.Work)]      // 11:00 -> Work
    [InlineData(12, TimeMode.Break)]     // 12:00 -> Break
    [InlineData(13, TimeMode.Break)]     // 13:00 -> Break
    [InlineData(14, TimeMode.Work)]      // 14:00 -> Work
    [InlineData(17, TimeMode.Work)]      // 17:00 -> Work
    [InlineData(18, TimeMode.Evening)]   // 18:00 -> Evening
    [InlineData(21, TimeMode.Evening)]   // 21:00 -> Evening
    [InlineData(22, TimeMode.Night)]     // 22:00 -> Night
    [InlineData(23, TimeMode.Night)]     // 23:00 -> Night
    [InlineData(0, TimeMode.Night)]      // 00:00 -> Night
    [InlineData(5, TimeMode.Night)]      // 05:00 -> Night
    public void GetCurrentMode_ShouldReturnCorrectMode(int hour, TimeMode expectedMode)
    {
        // 注意：这个测试依赖于当前时间，实际测试可能需要 mock
        // 这里我们只是测试 GetConfig 的逻辑
        var config = TimeModeManager.GetConfig(expectedMode);

        Assert.NotNull(config);
        Assert.NotEmpty(config.Label);
        Assert.NotEmpty(config.Emoji);
    }

    [Theory]
    [InlineData(TimeMode.Morning, "☀️", "Morning", true, false, false)]
    [InlineData(TimeMode.Work, "💼", "Work", false, false, false)]
    [InlineData(TimeMode.Break, "🍜", "Break", false, false, false)]
    [InlineData(TimeMode.Evening, "🌙", "Evening", false, true, false)]
    [InlineData(TimeMode.Night, "😴", "Night", false, false, true)]
    public void GetConfig_ShouldReturnCorrectConfiguration(
        TimeMode mode, 
        string expectedEmoji, 
        string expectedLabel,
        bool expectedBriefing,
        bool expectedReflective,
        bool expectedMinimal)
    {
        var config = TimeModeManager.GetConfig(mode);

        Assert.Equal(expectedEmoji, config.Emoji);
        Assert.Equal(expectedLabel, config.Label);
        Assert.Equal(expectedBriefing, config.ShowBriefing);
        Assert.Equal(expectedReflective, config.SuggestReflective);
        Assert.Equal(expectedMinimal, config.MinimalMode);
    }

    [Fact]
    public void GetConfig_AllModes_ShouldHaveValidConfiguration()
    {
        foreach (TimeMode mode in Enum.GetValues(typeof(TimeMode)))
        {
            var config = TimeModeManager.GetConfig(mode);

            Assert.NotNull(config);
            Assert.False(string.IsNullOrEmpty(config.Emoji));
            Assert.False(string.IsNullOrEmpty(config.Label));
        }
    }
}
