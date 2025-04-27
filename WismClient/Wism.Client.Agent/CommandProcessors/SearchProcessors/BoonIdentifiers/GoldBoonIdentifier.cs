using System;
using Wism.Client.Core.Boons;

namespace Wism.Client.Agent.CommandProcessors;

public class GoldBoonIdentifier : IBoonIdentifier
{
    public bool CanIdentify(IBoon boon)
    {
        return boon is GoldBoon;
    }

    public void Identify(IBoon boon)
    {
        if (!this.CanIdentify(boon))
        {
            throw new ArgumentException("Cannot identify " + boon);
        }

        var gold = (int)boon.Result;
        Notify.DisplayAndWait($"You have found {gold} gp!");
    }
}