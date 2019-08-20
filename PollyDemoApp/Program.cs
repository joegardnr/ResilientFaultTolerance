using Polly;
using Polly.Registry;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PollyDemoApp
{
    public class Program
    {
        public static Results ResultsLog = new Results();

        static async Task Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            Console.Clear();
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Polly Demo START!");
            Console.WriteLine("------------------------------------------------");
            stopwatch.Start();

            var samples = new PollySamples();
            samples.NoPolly_IntermittentlyBad();
            //samples.RetryForever();
            //samples.RetryForever_WithAccurateLogging();
            
            //samples.RetryForever_WithAccurateLogging_And_ReturnType();
            //samples.WaitAndRetry();
            //samples.WaitAndRetry_Escalating();

            //samples.RetryLimit_WithFallback();
            //samples.CircuitBreaker();

            //var registry = BuildRegistry_BasicExample();
            //samples.RetryUsingRegistry(registry);

            var samplesHttp = new PollyHttpSamples();
            //await samplesHttp.NoPollyAsync();
            //await samplesHttp.RetryForever_WithAccurateLogging();
            //await samplesHttp.CircuitBreaker();
            //await samplesHttp.RetryForever_With4xxError();


            stopwatch.Stop();
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine($"Runtime:             {stopwatch.Elapsed}");
            Console.WriteLine($"Total   Attempts:    {ResultsLog.TotalRequests}");
            Console.WriteLine($"Success Attempts:    {ResultsLog.Success}");
            Console.WriteLine($"Failed  Attempts:    {ResultsLog.Fail}");
            Console.WriteLine($"Skipped Attempts:    {ResultsLog.Skipped}");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Polly Demo END!");
            Console.WriteLine("------------------------------------------------");
            await Task.Delay(0);  // Avoid a warn while running the sync samples.
        }

        /// <summary>
        /// Define the Registry and populate it with a default try policy.
        /// You might do this in your app startup, or as part of configuring
        /// your dependency injection.
        /// </summary>
        /// <returns></returns>
        public static IPolicyRegistry<string> BuildRegistry_BasicExample()
        {
            var registry = new PolicyRegistry();

            var retry = Policy.Handle<Exception>().RetryForever((ex) =>
            {
                Program.ResultsLog.Fail++;
            });

            registry.Add("DefaultRetry", retry);

            return registry;
        }
    }
    public class Results
    {
        public int Success;
        public int Fail;
        public int Skipped;
        public int TotalRequests => Success + Fail + Skipped;        
    }
}
