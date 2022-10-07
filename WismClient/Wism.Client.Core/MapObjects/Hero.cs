using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Hero : Army
    {
        public const int MaxHeros = 10;
        public const int MinGoldToHire = 800;
        public const int MaxGoldToHire = 2000;

        public List<Artifact> Items { get; internal set; }

        internal Hero()
        {
            this.Info = ArmyInfo.GetHeroInfo();
            this.DisplayName = this.Info.DisplayName;
        }

        public void TakeAll()
        {
            if (this.Tile.HasItems())
            {
                Take(this.Tile.Items);
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
                !this.Tile.HasItems())
            {
                return;
            }

            foreach (var item in new List<Artifact>(items))
            {
                if (!this.Tile.Items.Contains(item))
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

            foreach (var item in new List<Artifact>(this.Items))
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
                this.Items.Contains(item))
            {
                item.Drop(this);
                this.Items.Remove(item);
                RecalculateCombatBonsuses();
            }
        }

        public string GetCompanionInteraction()
        {
            if (!HasCompanion())
            {
                return "You pet yourself when no one is looking.";
            }
            else
            {
                var companion = this.Items.Find(c => c.CompanionInteraction != null);

                // TODO: Allow only one companion per hero
                return companion.CompanionInteraction;
            }
        }

        public bool HasCompanion()
        {
            if (!HasItems())
            {
                return false;
            }

            var companion = this.Items.Find(c => c.CompanionInteraction != null);

            return companion != null;
        }

        private void RecalculateCombatBonsuses()
        {
            int totalStrength = this.Info.Strength + this.BlessedAt.Count;
            foreach (var item in this.Items)
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

            foreach (var item in this.Items)
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

            foreach (var item in this.Items)
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
