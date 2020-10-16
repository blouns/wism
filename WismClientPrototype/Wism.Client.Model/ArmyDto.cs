using System;

namespace Wism.Client.Model
{
    public class ArmyDto : MapObjectBaseDto
    {
        public int HitPoints { get; set; }

        public int Strength { get; set; }

        public void Attack(int x, int y)
        {
            if (HitPoints > 0)
            {
                HitPoints = HitPoints - 1;
            }
        }
    }
}
