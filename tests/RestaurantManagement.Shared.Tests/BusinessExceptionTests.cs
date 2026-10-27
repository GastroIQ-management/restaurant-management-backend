using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests;

/// <summary>A refusal cannot be raised without naming its code.</summary>
public class BusinessExceptionTests
{
    [Fact]
    [Trait("Rule", "SH57")]
    public void Constructors_AllPublic_RequireAnErrorCode()
    {
        // Act
        var constructors = typeof(BusinessException).GetConstructors();

        // Assert
        constructors.ShouldAllBe(
            constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ErrorCode)),
            "the compiler must refuse a new refusal that names no code");
    }
}
