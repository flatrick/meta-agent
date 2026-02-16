using Xunit;

public class GreetingTests
{
    [Fact]
    public void Build_ReturnsExpectedGreeting()
    {
        var greeting = Greeting.Build("demo-service");
        Assert.Equal("Hello from demo-service", greeting);
    }
}
