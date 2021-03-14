using System;
using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Hero : Army
    {
        public List<Artifact> Items { get; internal set; }

        internal Hero()
        {
            this.Info = ArmyInfo.GetHeroInfo();
            this.DisplayName = Info.DisplayName;
        }

        public void TakeAll()
        {
            if (Tile.HasItems())
            {
                Take(Tile.Items);
            }
        }

        public void Take(Artifact item)
        {
            if (item == null)
            {
                return;
            }

            item.Take(this);
            RecalculateCombatBonsuses();
        }

        public void Take(List<Artifact> items)
        {
            if (items is null ||
                !Tile.HasItems())
            {
                return;
            }

            foreach (var item in new List<Artifact>(items))
            {
                if (!Tile.Items.Contains(item))
                {
                    throw new ArgumentException("Item was not found on current tile: " + item);
                }

                Take(item);
            }
        }

        public void DropAll()
        {
            if (!HasItems())
            {
                return;
            }

            foreach (var item in new List<Artifact>(Items))
            {
                Drop(item);
            }
        }

        public void Drop(Artifact item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (HasItems() && 
                Items.Contains(item))
            {
                item.Drop(this);
                Items.Remove(item);
                RecalculateCombatBonsuses();
            }
        }

        private void RecalculateCombatBonsuses()
        {
            int totalStrength = Info.Strength + BlessedAt.Count;
            foreach (var item in Items)
            {
                totalStrength += item.CombatBonus;
            }

            if (totalStrength > MaxStrength)
            {
                totalStrength = MaxStrength;
            }

            this.Strength = totalStrength;
        }

        /// <summary>
        /// Drop all items when a hero is slain
        /// </summary>
        public override void Kill()
        {
            DropAll();
            base.Kill();
        }

        public int GetCommandBonus()
        {
            int bonus = 0;

            if (!HasItems())
            {
                return bonus;
            }
            
            foreach (var item in Items)
            {
                Artifact artifact = item as Artifact;
                if (artifact == null)
                {
                    continue;
                }    

                bonus += artifact.CommandBonus;
            }
            
            return bonus;
        }

        public int GetCombatBonus()
        {
            int bonus = 0;

            if (!HasItems())
            {
                return bonus;
            }

            foreach (var item in Items)
            {
                Artifact artifact = item as Artifact;
                if (artifact == null)
                {
                    continue;
                }

                bonus += artifact.CombatBonus;
            }

            return bonus;
        }

        public bool HasItems()
        {
            return this.Items != null && this.Items.Count > 0;
        }
    }
}
