using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors;

public class AlliesBoonIdentifier : IBoonIdentfier
{
    public bool CanIdentify(IBoon boon)
    {
        return boon is AlliesBoon;
    }

    public void Identify(IBoon boon)
    {
        if (!this.CanIdentify(boon))
        {
            throw new ArgumentException("Cannot identify " + boon);
        }

        var armies = (List<Army>)boon.Result;
        if (armies.Count > 1)
        {
            Notify.DisplayAndWait($"A {armies[0].DisplayName} has offered to join your party!");
        }
        else if (armies.Count > 0)
        {
            Notify.DisplayAndWait($"{armies.Count} {armies[0].DisplayName} have offered to join your party!");
        }
        else
        {
            throw new ArgumentException("No armies found in the AlliesBoon");
        }
    }
}