using System;
using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Hero : Army
    {
        public List<IItem> Items { get; internal set; }

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

        public void Take(List<IItem> items)
        {
            if (items is null)
            {
                throw new System.ArgumentNullException(nameof(items));
            }

            if (!Tile.HasItems())
            {
                throw new ArgumentException("Items are not available on current tile");
            }

            foreach (var item in new List<IItem>(items))
            {
                if (!Tile.Items.Contains(item))
                {
                    throw new ArgumentException("Item was not found on current tile: " + item);
                }

                item.Take(this);
                ApplyBonuses(item);
            }
        }

        private void ApplyBonuses(IItem item)
        {
            var artifact = item as Artifact;
            if (artifact == null)
            {
                return;
            }

            this.Strength += artifact.CombatBonus;
        }

        public void DropAll()
        {
            if (!HasItems())
            {
                return;
            }

            foreach (var item in new List<IItem>(Items))
            {
                Drop(item);
            }
        }

        public void Drop(IItem item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (HasItems() && 
                Items.Contains(item))
            {
                item.Drop(this);
                RemoveBonsuses(item);
            }
        }

        private void RemoveBonsuses(IItem item)
        {
            var artifact = item as Artifact;
            if (artifact == null)
            {
                return;
            }

            this.Strength -= artifact.CombatBonus;
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
