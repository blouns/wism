using System;
using Wism.Client.Core.Boons;

namespace Wism.Client.Agent.CommandProcessors.Human.SearchProcessors.BoonIdentifiers;

public class ThroneBoonIdentifier : IBoonIdentifier
{
    public bool CanIdentify(IBoon boon)
    {
        return boon is ThroneBoon;
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