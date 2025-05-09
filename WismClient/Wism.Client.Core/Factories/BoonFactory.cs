﻿using System;
using System.Reflection;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public class BoonFactory
    {
        public static IBoon Load(BoonEntity snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var assembly = Assembly.Load(snapshot.BoonAssemblyName);
            IBoon boon;

            if (!string.IsNullOrWhiteSpace(snapshot.AlliesShortName))
            {
                var armyInfo = ModFactory.FindArmyInfo(snapshot.AlliesShortName);
                boon = (IBoon)Activator.CreateInstance(
                    assembly.GetType(snapshot.BoonTypeName), armyInfo);
            }
            else if (!string.IsNullOrWhiteSpace(snapshot.ArtifactShortName))
            {
                var artifactInfo = ModFactory.FindArtifactInfo(snapshot.ArtifactShortName);
                var artifact = Artifact.Create(artifactInfo);
                boon = (IBoon)Activator.CreateInstance(
                    assembly.GetType(snapshot.BoonTypeName), artifact);
            }
            else
            {
                boon = (IBoon)Activator.CreateInstance(assembly.GetType(snapshot.BoonTypeName));
            }

            return boon;
        }
    }
}