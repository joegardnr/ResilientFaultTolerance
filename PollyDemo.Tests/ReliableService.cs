using Polly;
using Polly.Registry;
using System.Collections.Generic;
using System.Linq;

namespace PollyDemo.Tests
{
    public class ReliableService
    {
        public Dictionary<int, string> dbContext;  

        private IPolicyRegistry<string> _registry;

        public ReliableService(IPolicyRegistry<string> registry)
        {
            _registry = registry;

            // Demo Code. Pretend this is a real database.
            dbContext = new Dictionary<int, string>();
            dbContext[99] = "Test!";
        }

        /// <summary>
        /// All policies implement ISyncPolicy or IAsyncPolicy
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string UseRegistry(int input)
        {
            var policy = _registry.Get<ISyncPolicy>("DefaultRetry");
            var response = policy.Execute(
                () => dbContext.First(r => r.Key == input));
            return response.Value;
        }

        /// <summary>
        /// Protect yourself against a missing policy by using NoOp
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string UseRegistryWithNoOp(int input)
        {
            ISyncPolicy policy = 
                _registry.ContainsKey("DefaultRetry") 
                ? _registry.Get<ISyncPolicy>("DefaultRetry") 
                : Policy.NoOp();

            var response = policy.Execute(() => dbContext.First(r => r.Key == input));
            return response.Value;
        }

        /// <summary>
        /// This policy could be anything, including NoOp.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public string UseMethodInjection(int input, ISyncPolicy policy)
        {
            var response = policy.Execute(() => dbContext.First(r => r.Key == input));
            return response.Value;
        }
    }    
}
