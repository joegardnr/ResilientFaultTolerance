using System;
using System.Threading;

namespace PollyDemoApp
{
    public class UnreliableService
    {
        public int RequestCount = 0;
        public int StandardDelay = 100;
        public int IntermittentBadMod = 3;
        
        public int MostlyBad(int requestValue)
        {
            RequestCount++;
            Thread.Sleep(StandardDelay);
            if (RequestCount % IntermittentBadMod != 0)
            {
                throw new BadException("Intermittently Bad!");
            }
            return requestValue;
        }
        
        public int UpAndDown(int requestValue)
        {
            RequestCount++;
            Thread.Sleep(StandardDelay);
            if (DateTime.Now.Minute % 2 == 0)
            {
                throw new DownException("Down for a Moment!");
            }
            return requestValue;
        }

        public class BadException : Exception
        {
            public BadException(string message) : base(message) { }                
        }
        public class DownException : Exception
        {
            public DownException(string message) : base(message) { }
        }
    }
}
