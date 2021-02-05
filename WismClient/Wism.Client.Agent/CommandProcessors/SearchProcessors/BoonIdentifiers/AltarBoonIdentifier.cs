using System;
using Wism.Client.Core;

namespace Wism.Client.Agent.CommandProcessors
{
    public class AltarBoonIdentifier : IBoonIdentfier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is AltarBoon;
        }

        public void Identify(IBoon boon)
        {
            if (!CanIdentify(boon))
            {
                throw new ArgumentException("Cannot identify " + boon);
            }

            var strengthBoon = (int)boon.Result;
            if (strengthBoon == 0)
            {
                Console.WriteLine($"The gods ignore you!");
            }
            else if (strengthBoon > 0)
            {
                Console.WriteLine($"Your strength has increased by {strengthBoon}!");
            }
            else
            {
                Console.WriteLine($"Your strength decreased by {strengthBoon}");
            }
        }
    }
}
