using Polly;
using Polly.Registry;
using Xunit;

namespace PollyDemo.Tests
{
    public class ReliableService_Tests
    {
        [Fact]
        public void Test_Using_Registry()
        {
            // Arrange
            var registry = new PolicyRegistry();  // You could also Mock IPolicyRegistry if you wanted (but why?).
            registry.Add("DefaultRetry", Policy.NoOp());  
            
            var reliableService = new ReliableService(registry);

            // Act
            var result = reliableService.UseRegistry(99);

            // Assert
            Assert.Equal("Test!", result);
        }

        [Fact]
        public void Test_Using_UseRegistryWithNoOp()
        {
            // Arrange
            var registry = new PolicyRegistry();
            var reliableService = new ReliableService(registry);

            // Act
            var result = reliableService.UseRegistryWithNoOp(99);

            // Assert
            Assert.Equal("Test!", result);
        }

        [Fact]
        public void Test_Using_UseMethodInjection()
        {
            // Arrange            
            var noOpPolicy = Policy.NoOp();
            var reliableService = new ReliableService(null);

            // Act
            var result = reliableService.UseMethodInjection(99, noOpPolicy);

            // Assert
            Assert.Equal("Test!", result);
        }
    }
}
