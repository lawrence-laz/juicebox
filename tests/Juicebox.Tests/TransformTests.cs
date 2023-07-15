using FluentAssertions;
using JuiceboxEngine;
using JuiceboxEngine.Wip;
using Xunit;
using Transform = JuiceboxEngine.Wip.Transform;

namespace Juicebox.Tests;

public class TransformTests
{
    [Fact]
    public void GlobalPosition_WithParent_ShoulTakeIntoAccountParentPosition()
    {
        // Arrange
        var parent = new Transform
        {
            Position = new(10, 10)
        };
        var child = new Transform
        {
            Parent = parent,
            LocalPosition = new(5, 5)
        };

        // Act
        var actual = child.Position;

        // Assert
        actual.Should().Be(new Vector2(15, 15));
    }
}
