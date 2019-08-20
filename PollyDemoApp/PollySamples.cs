using System;
using System.Collections.Generic;
using System.Text;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Registry;
using Polly.Retry;

namespace PollyDemoApp
{
    public class PollySamples
    {
        private UnreliableService _service;        

        public PollySamples()
        {
            _service = new UnreliableService();
        }

        public void NoPolly_IntermittentlyBad()
        {
            for (int i=1; i<=20; i++)
            {
                int response = -1;
                try
                {
                    response = _service.MostlyBad(i);
                    Program.ResultsLog.Success++;
                }
                catch
                {
                    Program.ResultsLog.Fail++;
                }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Simplest approach, but how do I track failures?
        /// </summary>
        /// <returns></returns>
        public void RetryForever()
        {
            var policy = Policy
                          .Handle<Exception>()
                          .RetryForever();

            for (int i = 1; i <= 20; i++)
            {
                int response = -1;
                policy.Execute(() =>
                {
                    response = _service.MostlyBad(i);
                    Program.ResultsLog.Success++;
                });
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Accurate success/fail rates.
        /// </summary>
        /// <returns></returns>
        public void RetryForever_WithAccurateLogging()
        {
            var policy = Policy
                          .Handle<Exception>()
                          .RetryForever(onRetry: (ex) =>
                            {
                                Program.ResultsLog.Fail++;
                            });

            for (int i = 1; i <= 20; i++)
            {
                int response = -1;
                policy.Execute(() =>
                {
                    response = _service.MostlyBad(i);
                    Program.ResultsLog.Success++;
                });
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Policies can have return types.
        /// </summary>
        /// <returns></returns>
        public void RetryForever_WithAccurateLogging_And_ReturnType()
        {
            var policy = Policy<int>
                          .Handle<Exception>()
                          .RetryForever(onRetry: (dr) =>
                          {
                              Program.ResultsLog.Fail++;
                          });

            for (int i = 1; i <= 20; i++)
            {
                var response = policy.Execute(() => _service.MostlyBad(i));
                
                if (response == i) { Program.ResultsLog.Success++; }                
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Sometimes you need a little delay between tries.
        /// </summary>
        /// <returns></returns>
        public void WaitAndRetry()
        {            
            int wait = 100;
            var policy = Policy<int>
                          .Handle<Exception>()
                          .WaitAndRetryForever(
                            attempt => TimeSpan.FromMilliseconds(wait),
                            onRetry: (dr, duration) =>
                            {
                                Program.ResultsLog.Fail++;
                            });

            for (int i = 1; i <= 20; i++)
            {
                var response = policy.Execute(() => _service.MostlyBad(i));

                if (response == i) { Program.ResultsLog.Success++; }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Sometimes you need to increase the delay between tries.
        /// </summary>
        /// <returns></returns>
        public void WaitAndRetry_Escalating()
        {
            int wait = 100;  // start small!
            var policy = Policy<int>
                          .Handle<Exception>()
                          .WaitAndRetryForever(
                            attempt => TimeSpan.FromMilliseconds(wait * attempt),
                            onRetry: (dr, duration) =>
                            {
                                Program.ResultsLog.Fail++;
                            });

            for (int i = 1; i <= 20; i++)
            {
                var response = policy.Execute(() => _service.MostlyBad(i));
                if (response == i) { Program.ResultsLog.Success++; }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Combines two scenarios:
        ///   Retry a fixed number of times,
        ///   but if that fails, give them something real
        ///   (not null or an exception)
        /// </summary>
        /// <returns></returns>
        public void RetryLimit_WithFallback()
        {
            int wait = 100;
            var retryPolicy = Policy<int>
                          .Handle<Exception>()
                          .WaitAndRetry(
                            retryCount: 1,
                            attempt => TimeSpan.FromMilliseconds(wait),
                            onRetry: (dr, duration) =>
                            {
                                Program.ResultsLog.Fail++;
                            });

            var fallbackPolicy = Policy<int>
                                 .Handle<Exception>()
                                 .Fallback<int>(-1);
            
            var policyWrap = Policy.Wrap(fallbackPolicy, retryPolicy);

            for (int i = 1; i <= 20; i++)
            {                
                var response = policyWrap.Execute(() => _service.MostlyBad(i));
                if(response == i) { Program.ResultsLog.Success++; }
                else if(response == -1) { Program.ResultsLog.Fail++; }
                
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        /// <summary>
        /// Use the Circuit Breaker to protect the downstream service,
        /// while the Retry Policy valiantly keeps going and going and going...
        /// </summary>
        /// <returns></returns>
        public void CircuitBreaker()
        {
            var threshold = 3; var wait = 100; var durationOfBreak = TimeSpan.FromSeconds(30);          

            var circuitBreakerPolicy = Policy<int>
                                        .Handle<Exception>()
                                        .CircuitBreaker(threshold, durationOfBreak);

            var retryPolicy = Policy<int>
                               .Handle<Exception>()
                               .WaitAndRetryForever(
                                attempt => TimeSpan.FromMilliseconds(wait),
                                onRetry: (dr, duration) =>
                                {
                                    if (dr.Exception is BrokenCircuitException) { Program.ResultsLog.Skipped++; }
                                    else { Program.ResultsLog.Fail++; }
                                });            

            var policyWrap = Policy.Wrap(retryPolicy, circuitBreakerPolicy);

            for (int i = 1; i <= 20; i++)
            {
                var response = policyWrap.Execute(() =>
                {
                    return _service.UpAndDown(i);
                });

                if (response == i) { Program.ResultsLog.Success++; }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }       

        /// <summary>
        /// Inject a registry created elsewhere, and execute a policy from it.
        /// </summary>
        /// <param name="registry"></param>
        /// <returns></returns>
        public void RetryUsingRegistry(IPolicyRegistry<string> registry)
        {
            var results = new Results();
            var policy = registry.Get<RetryPolicy>("DefaultRetry");

            for (int i = 1; i <= 20; i++)
            {
                int response = -1;
                policy.Execute(() =>
                {
                    response = _service.MostlyBad(i);
                    Program.ResultsLog.Success++;
                });
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }
    }
}
