using System;
using System.Reflection;
using Wism.Client.Core;
using Wism.Client.Entities;
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
            
            if (!String.IsNullOrWhiteSpace(snapshot.AlliesShortName))
            {
                var armyInfo = ModFactory.FindArmyInfo(snapshot.AlliesShortName);
                boon = (IBoon)Activator.CreateInstance(
                    assembly.GetType(snapshot.BoonTypeName), armyInfo);                
            }
            else if (!String.IsNullOrWhiteSpace(snapshot.ArtifactShortName))
            {
                var artifactInfo = ModFactory.FindArtifactInfo(snapshot.ArtifactShortName);
                boon = (IBoon)Activator.CreateInstance(
                    assembly.GetType(snapshot.BoonTypeName), artifactInfo);
            }
            else
            {
                boon = (IBoon)Activator.CreateInstance(assembly.GetType(snapshot.BoonTypeName));
            }

            return boon;
        }
    }
}