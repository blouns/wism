using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// <summary>
    ///     COMBAT MECHANICS
    ///     Combat occurs whenever opposing armies(or stacks of armies) contest the
    ///     Ownership of a particular location.The computer follows a set routine for
    ///     each combat.
    ///     (1). An Attacking Force Combat Modifier(AFCM) is calculated.Five factors
    ///     are evaluated when calculating the AFCM.
    ///     (a). Hero Present.If the hero's combat strength is 0 to 3, the modifier
    ///     is 0. If the hero's combat strength is 4 to 6, the modifier is 1. If the
    ///     hero's combat strength is 7 or 8, the modifier is 2. If the hero's combat
    ///     strength is 9, the modifier is 3.
    ///     (b). Flying Army Present.If a Pegasus, Griffin or Dragon is present, the
    ///     modifier is 1.
    ///     (c). Special Army Present.If a Wizard, Undead, Demon, Devil or Dragon is
    ///     present, the modifier is 1.
    ///     (d). Command Item Present. If a hero (or heroes) with command item(s) are
    ///     present, the value of the command item(s) is added.For example, the Crimson
    ///     Banner has a command value of 2.
    ///     (e). Terrain Modifier. Troops from the different Empires have their likes
    ///     and dislikes in regard to where they prefer to fight.For example, the
    ///     Elvallie like forests but don't much care for hills or marsh. The Sirians
    ///     don't mind where they fight. Consult the following table for the correct
    ///     modifier.
    ///     These modifiers, if any, are added together. Once calculated, the AFCM is
    ///     set aside for use later in the combat routine.
    ///     TERRAIN TYPE
    ///     F
    ///     W  S  O     P  M
    ///     R  A  H  R  H  L  A
    ///     O  T  O  E  I  A  R
    ///     A  E  R  S  L  I  S
    ///     D  R  E  T  L  N  H
    ///     +---------------------+
    ///     SIRIANS           : .  .  .  .  .  .  . :
    ///     STORM GIANTS      : .  .  .  . +1  . -1 :
    ///     GREY DWARVES      : .  .  . -1 +2  . -1 :
    ///     ORCS OF KOR       : .  .  . -1  .  . +1 :
    ///     ELVALLIE          : .  .  . +1 -1  . -1 :
    ///     SELENTINES        : . +1 +1  .  .  .  . :
    ///     HORSE LORDS       :+1  .  . -1 -1 +1  . :
    ///     LORD BANE         : .  .  . -1  .  . +1 :
    ///     +---------------------+
    ///     .=NO EFFECT
    /// </summary>
    public class AttackingForceCombatModifier : ICombatModifier
    {
        private readonly List<ICombatModifier> modifiers = new List<ICombatModifier>();

        public AttackingForceCombatModifier()
        {
            // Standard Warlords rules
            this.modifiers.Add(new HeroPresentAFCM());
            this.modifiers.Add(new FlyingArmyPresentAFCM());
            this.modifiers.Add(new SpecialArmyPresentAFCM());
            this.modifiers.Add(new CommandItemPresentAFCM());
            this.modifiers.Add(new TerrainAFCM());
        }

        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            var compositeModifer = this.modifiers.Sum(v => v.Calculate(attacker, target, modifier));

            return modifier + compositeModifer;
        }
    }
}