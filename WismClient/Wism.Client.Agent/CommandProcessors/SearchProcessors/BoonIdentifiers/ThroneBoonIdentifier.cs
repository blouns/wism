using System;
using Wism.Client.Core;

namespace Wism.Client.Agent.CommandProcessors;

public class ThroneBoonIdentifier : IBoonIdentfier
{
    public bool CanIdentify(IBoon boon)
    {
        return boon is ThroneBoon;
    }

    public void Identify(IBoon boon)
    {
        if (!this.CanIdentify(boon))
        {
            throw new ArgumentException("Cannot identify " + boon);
        }

        var strengthBoon = (int)boon.Result;
        if (strengthBoon == 0)
        {
            Notify.DisplayAndWait("The gods ignore you!");
        }
        else if (strengthBoon > 0)
        {
            Notify.DisplayAndWait($"Your strength has increased by {strengthBoon}!");
        }
        else
        {
            Notify.DisplayAndWait($"Your strength decreased by {strengthBoon}");
        }
    }
}