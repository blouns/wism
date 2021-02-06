using System;
using Wism.Client.Core;

namespace Wism.Client.Agent.CommandProcessors
{
    public class GoldBoonIdentifier : IBoonIdentfier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is GoldBoon;
        }

        public void Identify(IBoon boon)
        {
            if (!CanIdentify(boon))
            {
                throw new ArgumentException("Cannot identify " + boon);
            }

            var gold = (int)boon.Result;
            Notify.DisplayAndWait($"You have found {gold} gp!");
        }
    }
}
