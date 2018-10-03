using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class UnitComposite : Unit
    {
        IList<Unit> units;

        public static UnitComposite Create(IList<Unit> units)
        {
            UnitComposite composite = new UnitComposite();
            composite.units = units ?? throw new ArgumentNullException(nameof(units));

            return composite;
        }

        public UnitComposite()
        {
        }

        public override IList<Unit> Expand()
        {
            return new List<Unit>(units);
        }

        public override bool IsSpecial()
        {
            return units.Any<Unit>(v => v.IsSpecial());
        }

        public override string DisplayName => "Army";

        public override char Symbol { get => 'A'; set => base.Symbol = value; }

        public override int GetAttackModifier()
        {
            return units.Sum<Unit>(v => v.GetAttackModifier());
        }

        public override int GetDefenseModifier()
        {
            return units.Sum<Unit>(v => v.GetDefenseModifier());
        }
    }
}
