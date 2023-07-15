using Xunit;

namespace Juicebox.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var aaaa = 1;
        var bbbbb = 2;

        Assert.Equal(bbbbb, aaaa + 1);
    }

    [Fact]
    public void Test4()
    {
        var aaaa = 1;
        var bbbbb = 2;

        Assert.Equal(bbbbb, aaaa + 1);
    }

    [Fact]
    public void Test2()
    {
        // What is this?
    }

    [Fact]
    public void Test3()
    {
        var aaaa = 1;
        var bbbbb = 2;

        Assert.Equal(bbbbb, aaaa);
    }
}
