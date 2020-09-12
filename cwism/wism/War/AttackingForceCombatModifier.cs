using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    /// <summary>
    /// Interface to define combat modifiers for calculating bonuses.
    /// </summary>
    interface ICombatModifier
    {
        /// <summary>
        /// Calculate the bonus modifer for the attacker in the target terrain
        /// </summary>
        /// <param name="attacker">Attacking unit of an army</param>
        /// <param name="target">Tile being attacked</param>
        /// <param name="modifier">Internal composite modifier</param>
        /// <returns>Aggregate modifier bonus</returns>
        int Calculate(Unit attacker, Tile target, int modifier = 0);
    }

    /// <summary>
    /// COMBAT MECHANICS
    /// Combat occurs whenever opposing armies(or stacks of armies) contest the
    /// Ownership of a particular location.The computer follows a set routine for
    /// each combat.
    ///
    /// (1). An Attacking Force Combat Modifier(AFCM) is calculated.Five factors
    /// are evaluated when calculating the AFCM.
    ///
    /// (a). Hero Present.If the hero's combat strength is 0 to 3, the modifier
    /// is 0. If the hero's combat strength is 4 to 6, the modifier is 1. If the
    /// hero's combat strength is 7 or 8, the modifier is 2. If the hero's combat
    /// strength is 9, the modifier is 3.
    ///
    /// (b). Flying Army Present.If a Pegasus, Griffin or Dragon is present, the
    /// modifier is 1.
    ///
    /// (c). Special Army Present.If a Wizard, Undead, Demon, Devil or Dragon is
    /// present, the modifier is 1.
    ///
    /// (d). Command Item Present. If a hero (or heroes) with command item(s) are
    /// present, the value of the command item(s) is added.For example, the Crimson
    /// Banner has a command value of 2.
    ///
    /// (e). Terrain Modifier. Troops from the different Empires have their likes
    /// and dislikes in regard to where they prefer to fight.For example, the
    /// Elvallie like forests but don't much care for hills or marsh. The Sirians
    /// don't mind where they fight. Consult the following table for the correct
    /// modifier.
    /// These modifiers, if any, are added together. Once calculated, the AFCM is
    /// set aside for use later in the combat routine.
    ///
    ///                                         TERRAIN TYPE
    ///
    ///                                             F
    ///                                       W  S  O     P  M
    ///                                    R  A  H  R  H  L  A
    ///                                    O  T  O  E  I  A  R
    ///                                    A  E  R  S  L  I  S
    ///                                    D  R  E  T  L  N  H
    ///                                  +---------------------+
    ///                SIRIANS           : .  .  .  .  .  .  . :
    ///                STORM GIANTS      : .  .  .  . +1  . -1 :     
    ///                GREY DWARVES      : .  .  . -1 +2  . -1 :
    ///                ORCS OF KOR       : .  .  . -1  .  . +1 :
    ///                ELVALLIE          : .  .  . +1 -1  . -1 :
    ///                SELENTINES        : . +1 +1  .  .  .  . :
    ///                HORSE LORDS       :+1  .  . -1 -1 +1  . :
    ///                LORD BANE         : .  .  . -1  .  . +1 :
    ///                                  +---------------------+
    ///                                             .=NO EFFECT
    /// </summary>
    public class AttackingForceCombatModifier : ICombatModifier
    {
        IList<ICombatModifier> modifiers = new List<ICombatModifier>();

        public AttackingForceCombatModifier()
        {
            // Standard Warlords rules
            modifiers.Add(new HeroPresentAFCM());
            modifiers.Add(new FlyingArmyPresentAFCM());
            modifiers.Add(new SpecialArmyPresentAFCM());
            modifiers.Add(new CommandItemPresentAFCM());
            modifiers.Add(new TerrainAFCM());
        }

        public int Calculate(Unit attacker, Tile target, int modifier = 0)
        {
            int compositeModifer = modifiers.Sum<ICombatModifier>(v => v.Calculate(attacker, target, modifier));

            return modifier + compositeModifer;
        }        
    }

    /// <summary>
    /// Hero Present. If the hero's combat strength is 0 to 3, the modifier
    //  is 0. If the hero's combat strength is 4 to 6, the modifier is 1. If the
    //  hero's combat strength is 7 or 8, the modifier is 2. If the hero's combat
    //  strength is 9, the modifier is 3.
    /// </summary>
    public class HeroPresentAFCM : ICombatModifier
    {
        public int Calculate(Unit attacker, Tile target, int modifier = 0)
        {            
            if (attacker is Hero)
            {
                if (attacker.Strength <= 3)
                    modifier += 0;
                else if (attacker.Strength >= 4 && attacker.Strength <= 6)
                    modifier += 1;
                else if (attacker.Strength >= 7 && attacker.Strength <= 8)
                    modifier += 2;
                else if (attacker.Strength >= 9)
                    modifier += 3;
            }

            return modifier;
        }
    }

    /// <summary>
    /// Flying Army Present.If a Pegasus, Griffin or Dragon is present, the
    /// modifier is 1.
    /// </summary>
    public class FlyingArmyPresentAFCM : ICombatModifier
    {
        public int Calculate(Unit attacker, Tile target, int modifier = 0)
        {
            if (attacker.CanFly)
            {
                return modifier++;
            }

            return modifier;
        }
    }

    /// <summary>
    /// Special Army Present. If a Wizard, Undead, Demon, Devil or Dragon is
    /// present, the modifier is 1.
    /// </summary>
    public class SpecialArmyPresentAFCM : ICombatModifier
    {
        public int Calculate(Unit attacker, Tile target, int modifier = 0)
        {
            if (attacker.IsSpecial())
            {
                modifier++;
            }

            return modifier;
        }
    }

    /// Command Item Present. If a hero (or heroes) with command item(s) are
    /// present, the value of the command item(s) is added.For example, the Crimson
    /// Banner has a command value of 2.
    public class CommandItemPresentAFCM : ICombatModifier
    {
        public int Calculate(Unit attacker, Tile target, int modifier = 0)
        {
            Hero hero = attacker as Hero;
            if (hero != null)
            {                
                modifier += hero.GetCommandBonus();
            }

            return modifier;
        }
    }

    /// <summary>
    /// Terrain Modifier. Troops from the different Empires have their likes
    /// and dislikes in regard to where they prefer to fight. For example, the
    /// Elvallie like forests but don't much care for hills or marsh. The Sirians
    /// don't mind where they fight. Consult the following table for the correct
    /// modifier.
    /// </summary>
    public class TerrainAFCM : ICombatModifier
    {
        public int Calculate(Unit attacker, Tile target, int modifier = 0)
        {
            return attacker.Affiliation.GetTerrainModifier(target);            
        }
    }
}
