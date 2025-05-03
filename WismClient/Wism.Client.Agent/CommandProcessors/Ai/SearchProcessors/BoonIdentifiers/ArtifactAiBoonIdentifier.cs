using System;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors.Ai.SearchProcessors.BoonIdentifiers;

public class ArtifactAiBoonIdentifier : IBoonIdentifier
{
    public bool CanIdentify(IBoon boon)
    {
        return boon is ArtifactBoon;
    }

    public void Identify(IBoon boon)
    {
        if (!CanIdentify(boon))
        {
            throw new ArgumentException("Cannot identify " + boon);
        }

        var artifact = (Artifact)boon.Result;
        Notify.DisplayAndWait($"You have found the {artifact.DisplayName}!");
    }
}