using System;
using Wism.Client.Core.Boons;

namespace Wism.Client.Agent.CommandProcessors.Human.SearchProcessors.BoonIdentifiers;

public class GoldBoonIdentifier : IBoonIdentifier
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