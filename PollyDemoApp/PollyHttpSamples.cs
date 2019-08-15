using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PollyDemoApp
{
    public class PollyHttpSamples
    {
        private HttpClient _client;

        public PollyHttpSamples()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:5000/api/simulation/");
        }

        public async Task NoPollyAsync()
        {
            for (int i = 1; i <= 20; i++)
            {
                var response = await _client.GetAsync($"intermittent");

                if (response.StatusCode == System.Net.HttpStatusCode.OK) { Program.ResultsLog.Success++; }
                else { Program.ResultsLog.Fail++; }

                //Console.WriteLine($"Request {request} | Response {response.StatusCode}");
            }
        }

        public async Task RetryForever_WithAccurateLogging()
        {
            var policy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                          .RetryForeverAsync((resp) =>
                          {
                            Program.ResultsLog.Fail++;
                          });

            for (int i = 1; i <= 20; i++)
            {                
                var response = await policy.ExecuteAsync( async () =>
                {
                    return await _client.GetAsync($"intermittent");
                });
                if(response.StatusCode == HttpStatusCode.OK) { Program.ResultsLog.Success++; }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        public async Task CircuitBreaker()
        {
            var threshold = 3; var wait = 100; var durationOfBreak = TimeSpan.FromSeconds(30);

            var retryPolicy = HttpPolicyExtensions
                               .HandleTransientHttpError()
                               .Or<BrokenCircuitException>()
                               .WaitAndRetryForeverAsync(
                                attempt => TimeSpan.FromMilliseconds(wait),
                                onRetry: (dr, duration) =>
                                {
                                    if (dr.Exception is BrokenCircuitException) { Program.ResultsLog.Skipped++; }
                                    else { Program.ResultsLog.Fail++; }
                                });

            var circuitBreakerPolicy = HttpPolicyExtensions
                                        .HandleTransientHttpError()
                                        .CircuitBreakerAsync(threshold, durationOfBreak);

            var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

            for (int i = 1; i <= 20; i++)
            {
                var response = await policyWrap.ExecuteAsync(async () =>
                {
                    return await _client.GetAsync($"updown");
                });
                if (response.StatusCode == HttpStatusCode.OK) { Program.ResultsLog.Success++; }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }

        public async Task RetryForever_With4xxError()
        {
            var policy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                          .OrResult(r => r.StatusCode == HttpStatusCode.BadRequest)
                          .RetryForeverAsync((resp) =>
                          {
                              Program.ResultsLog.Fail++;
                          });

            for (int i = 1; i <= 20; i++)
            {
                var response = await policy.ExecuteAsync(async () =>
                {
                    return await _client.GetAsync($"badrequest");
                });
                if (response.StatusCode == HttpStatusCode.OK) { Program.ResultsLog.Success++; }
                //Console.WriteLine($"Request {i} | Response {response}");
            }
        }
    }
}
