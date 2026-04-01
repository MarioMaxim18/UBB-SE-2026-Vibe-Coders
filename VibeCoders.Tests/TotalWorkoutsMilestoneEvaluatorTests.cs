using VibeCoders.Domain;

namespace VibeCoders.Tests;

/// <summary>
/// Unit tests for <see cref="TotalWorkoutsMilestoneEvaluator"/> (#186).
/// All tests are pure (no DB, no DI) — the evaluator takes only an int.
/// </summary>
public sealed class TotalWorkoutsMilestoneEvaluatorTests
{
    // ── Custom table used by most tests ──────────────────────────────────────
    private static readonly IReadOnlyList<WorkoutMilestone> TestMilestones =
    [
        new WorkoutMilestone(Threshold: 1,  Title: "First",   Description: "1 workout"),
        new WorkoutMilestone(Threshold: 5,  Title: "Five",    Description: "5 workouts"),
        new WorkoutMilestone(Threshold: 10, Title: "Ten",     Description: "10 workouts"),
    ];

    private readonly TotalWorkoutsMilestoneEvaluator _sut;

    public TotalWorkoutsMilestoneEvaluatorTests()
    {
        _sut = new TotalWorkoutsMilestoneEvaluator(TestMilestones);
    }

    // ── GetEarnedMilestones ──────────────────────────────────────────────────

    [Fact]
    public void GetEarnedMilestones_zero_workouts_returns_empty()
    {
        var earned = _sut.GetEarnedMilestones(0);
        Assert.Empty(earned);
    }

    [Fact]
    public void GetEarnedMilestones_exactly_first_threshold_returns_one()
    {
        var earned = _sut.GetEarnedMilestones(1);
        Assert.Single(earned);
        Assert.Equal("First", earned[0].Title);
    }

    [Fact]
    public void GetEarnedMilestones_between_thresholds_returns_lower_only()
    {
        var earned = _sut.GetEarnedMilestones(3);
        Assert.Single(earned);
        Assert.Equal("First", earned[0].Title);
    }

    [Fact]
    public void GetEarnedMilestones_exactly_on_second_threshold_returns_two()
    {
        var earned = _sut.GetEarnedMilestones(5);
        Assert.Equal(2, earned.Count);
        Assert.Equal("First", earned[0].Title);
        Assert.Equal("Five",  earned[1].Title);
    }

    [Fact]
    public void GetEarnedMilestones_above_all_thresholds_returns_all()
    {
        var earned = _sut.GetEarnedMilestones(100);
        Assert.Equal(TestMilestones.Count, earned.Count);
    }

    [Fact]
    public void GetEarnedMilestones_result_is_ordered_ascending_by_threshold()
    {
        var earned = _sut.GetEarnedMilestones(10);
        var thresholds = earned.Select(m => m.Threshold).ToList();
        Assert.Equal(thresholds.OrderBy(t => t).ToList(), thresholds);
    }

    [Fact]
    public void GetEarnedMilestones_negative_count_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetEarnedMilestones(-1));
    }

    // ── GetNewlyEarnedMilestones ─────────────────────────────────────────────

    [Fact]
    public void GetNewlyEarnedMilestones_crossing_first_threshold_returns_it()
    {
        var newlyEarned = _sut.GetNewlyEarnedMilestones(previousCount: 0, newCount: 1);
        Assert.Single(newlyEarned);
        Assert.Equal("First", newlyEarned[0].Title);
    }

    [Fact]
    public void GetNewlyEarnedMilestones_already_past_threshold_returns_empty()
    {
        // Already had 1 workout before; going to 3 crosses nothing new.
        var newlyEarned = _sut.GetNewlyEarnedMilestones(previousCount: 1, newCount: 3);
        Assert.Empty(newlyEarned);
    }

    [Fact]
    public void GetNewlyEarnedMilestones_crossing_multiple_at_once_returns_all()
    {
        // Going from 0 to 10 should cross all three thresholds (1, 5, 10).
        var newlyEarned = _sut.GetNewlyEarnedMilestones(previousCount: 0, newCount: 10);
        Assert.Equal(3, newlyEarned.Count);
    }

    [Fact]
    public void GetNewlyEarnedMilestones_new_less_than_previous_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _sut.GetNewlyEarnedMilestones(previousCount: 5, newCount: 3));
    }

    [Fact]
    public void GetNewlyEarnedMilestones_same_count_returns_empty()
    {
        var newlyEarned = _sut.GetNewlyEarnedMilestones(previousCount: 5, newCount: 5);
        Assert.Empty(newlyEarned);
    }

    // ── DefaultMilestones table ──────────────────────────────────────────────

    [Theory]
    [InlineData(1,   "First Steps")]
    [InlineData(10,  "Deca Athlete")]
    [InlineData(25,  "Quarter Century")]
    [InlineData(50,  "Half Century")]
    [InlineData(100, "Centurion")]
    public void DefaultMilestones_contains_expected_thresholds(int threshold, string title)
    {
        var match = TotalWorkoutsMilestoneEvaluator.DefaultMilestones
            .Single(m => m.Threshold == threshold);
        Assert.Equal(title, match.Title);
    }

    [Fact]
    public void DefaultMilestones_evaluator_first_steps_earned_at_one_workout()
    {
        var defaultEval = new TotalWorkoutsMilestoneEvaluator();
        var earned = defaultEval.GetEarnedMilestones(1);
        Assert.Contains(earned, m => m.Title == "First Steps");
    }

    [Fact]
    public void DefaultMilestones_evaluator_centurion_not_earned_at_99()
    {
        var defaultEval = new TotalWorkoutsMilestoneEvaluator();
        var earned = defaultEval.GetEarnedMilestones(99);
        Assert.DoesNotContain(earned, m => m.Title == "Centurion");
    }

    [Fact]
    public void DefaultMilestones_evaluator_centurion_earned_at_100()
    {
        var defaultEval = new TotalWorkoutsMilestoneEvaluator();
        var earned = defaultEval.GetEarnedMilestones(100);
        Assert.Contains(earned, m => m.Title == "Centurion");
    }
}
