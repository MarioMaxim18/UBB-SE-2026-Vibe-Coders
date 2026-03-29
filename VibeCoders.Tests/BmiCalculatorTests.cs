using VibeCoders.Domain;

namespace VibeCoders.Tests;


public sealed class BmiCalculatorTests
{
    [Fact]
    public void Zero_weight_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BmiCalculator.Calculate(0, 170));
    }

    [Fact]
    public void Negative_weight_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BmiCalculator.Calculate(-10, 170));
    }

    [Fact]
    public void Zero_height_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BmiCalculator.Calculate(70, 0));
    }

    [Fact]
    public void Negative_height_throws_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BmiCalculator.Calculate(70, -5));
    }

    [Theory]
    [InlineData(70, 175, 22.86)]   // typical healthy adult
    [InlineData(90, 180, 27.78)]   // overweight range
    [InlineData(50, 160, 19.53)]   // lower-normal range
    [InlineData(120, 170, 41.52)]  // obese range
    public void Known_inputs_produce_expected_bmi(
        double weightKg, double heightCm, double expectedBmi)
    {
        var result = BmiCalculator.Calculate(weightKg, heightCm);
        Assert.Equal(expectedBmi, result);
    }

    [Fact]
    public void Result_is_rounded_to_two_decimal_places()
    {
        // 70 / (1.75)^2 = 22.8571..., rounds to 22.86
        var result = BmiCalculator.Calculate(70, 175);
        Assert.Equal(2, BitConverter.GetBytes(decimal.GetBits((decimal)result)[3])[2]);
    }
}
