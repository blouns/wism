using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Api.Data.Entities;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Data
{
    public static class CommandPersistance
    {
        public static CommandEntity[] SnapshotCommands(List<Command> commands)
        {
            if (commands is null || commands.Count == 0)
            {
                return null;
            }

            var snapshot = new CommandEntity[commands.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = SnapshotCommand(commands[i]);
            }

            return snapshot;
        }

        private static CommandEntity SnapshotCommand(Command command)
        {
            CommandEntity snapshot;

            // Army command
            snapshot = SnapshotArmyCommand(command);

            // City command
            if (snapshot == null)
            {
                snapshot = SnapshotGameCommand(command);
            }

            // Game command
            if (snapshot == null)
            {
                snapshot = SnapshotGameCommand(command);
            }

            // Hero command
            if (snapshot == null)
            {
                snapshot = SnapshotHeroCommand(command);
            }

            // Location command
            if (snapshot == null)
            {
                snapshot = SnapshotLocationCommand(command);
            }

            // Player command
            if (snapshot == null)
            {
                snapshot = SnapshotPlayerCommand(command);
            }

            // Command details
            snapshot.Id = command.Id;
            snapshot.PlayerIndex = GetPlayerIndex(command.Player);
            snapshot.Result = command.Result;

            return snapshot;
        }

        private static CommandEntity SnapshotPlayerCommand(Command command)
        {
            CommandEntity snapshot = null;

            if (command is EndTurnCommand)
            {
                snapshot = new TurnCommandEntity()
                {
                    Starting = false
                };
            }
            else if (command is StartTurnCommand)
            {
                snapshot = new TurnCommandEntity()
                {
                    Starting = true
                };
            }

            return snapshot;
        }

        private static CommandEntity SnapshotLocationCommand(Command command)
        {
            CommandEntity snapshot = null;

            if (command.GetType().IsSubclassOf(typeof(SearchLocationCommand)))
            {
                if (command is SearchLibraryCommand)
                {
                    var subCommand = (SearchLibraryCommand)command;
                    snapshot = new SearchLibraryCommandEntity()
                    {
                        Knowledge = subCommand.Knowledge
                    };
                }
                else if (command is SearchRuinsCommand)
                {
                    var subCommand = (SearchRuinsCommand)command;
                    var boon = subCommand.Boon;
                    snapshot = new SearchRuinsCommandEntity()
                    {
                        Boon = GamePersistance.SnapshotBoon(subCommand.Boon),
                        AllyIdsResult = ConvertBoonToAlliesIds(boon.Result),
                        ArtifactShortNameResult = ConvertBoonToArtifactName(boon.Result),
                        GoldResult = (boon is GoldBoon) ? (int?)boon.Result : null,
                        StrengthResult = (boon is ThroneBoon) ? (int?)boon.Result : null,                       
                    };
                }
                else if (command is SearchSageCommand)
                {
                    var subCommand = (SearchSageCommand)command;
                    snapshot = new SearchSageCommandEntity()
                    {
                        Gold = subCommand.Gold
                    };
                }
                else if (command is SearchTempleCommand)
                {
                    var subCommand = (SearchTempleCommand)command;
                    snapshot = new SearchTempleCommandEntity()
                    {
                        BlessedArmyCount = subCommand.BlessedArmyCount
                    };
                }

                // General properties
                var locationCommand = (SearchLocationCommand)command;
                var locationEntity = (SearchLocationCommandEntity)snapshot;
                if (locationCommand.Location != null)
                {
                    locationEntity.LocationShortName = locationCommand.Location.ShortName;
                    locationEntity.ArmyIds = ConvertToArmyIds(locationCommand.Armies);
                };
            }

            return snapshot;
        }

        private static string ConvertBoonToArtifactName(object result)
        {
            if (result is Artifact)
            {
                return ((Artifact)result).ShortName;
            }

            return null;
        }

        private static int[] ConvertBoonToAlliesIds(object result)
        {
            if (result is Army[])
            {
                var allies = (Army[])result;
                var allyIds = new int[allies.Length];
                for (int i = 0; i < allies.Length; i++)
                {
                    allyIds[i] = allies[i].Id;
                }
            }

            return null;
        }

        private static CommandEntity SnapshotHeroCommand(Command command)
        {
            CommandEntity snapshot = null;
            if (command is TakeItemsCommand)
            {
                var subCommand = (TakeItemsCommand)command;
                snapshot = new ItemsCommandEntity()
                {
                    HeroId = subCommand.Hero.Id,
                    ItemShortNames = ConvertToItemNames(subCommand.Items),
                    Taking = true
                };
            }
            else if (command is DropItemsCommand)
            {
                var subCommand = (DropItemsCommand)command;
                snapshot = new ItemsCommandEntity()
                {
                    HeroId = subCommand.Hero.Id,
                    ItemShortNames = ConvertToItemNames(subCommand.Items),
                    Taking = false
                };
            }

            return snapshot;
        }

        private static CommandEntity SnapshotGameCommand(Command command)
        {
            CommandEntity snapshot = null;
            if (command is LoadGameCommand)
            {
                snapshot = new LoadGameCommandEntity()
                {
                    // TODO: Do NOT snapshot the snapshot. Need to think through the 
                    //       consequences of this and how to address it properly.
                    //Snapshot = ((LoadGameCommand)command).Snapshot
                };
            }
            else if (command is NewGameCommand)
            {
                snapshot = new NewGameCommandEntity()
                {
                    // TODO: Do NOT snapshot the new game. Need to think through the 
                    //       consequences of this and how to address it properly.
                    //Snapshot = ((NewGameCommand)command).Snapshot
                };
            }

            return snapshot;
        }

        private static CommandEntity SnapshotArmyCommand(Command command)
        {
            CommandEntity snapshot = null;
            if (command.GetType().IsSubclassOf(typeof(ArmyCommand)))
            {
                // Create appriate army command entity
                if (command is AttackOnceCommand)
                {
                    var subCommand = (AttackOnceCommand)command;
                    snapshot = new AttackCommandEntity()
                    {
                        DefendingArmyIds = ConvertToArmyIds(subCommand.Defenders),
                        OriginalAttackingArmyIds = ConvertToArmyIds(subCommand.OriginalAttackingArmies),
                        OriginalDefendingArmyIds = ConvertToArmyIds(subCommand.OriginalDefendingArmies)
                    };
                }
                else if (command is CompleteBattleCommand)
                {
                    var subCommand = (CompleteBattleCommand)command;
                    snapshot = new CompleteBattleCommandEntity()
                    {
                        AttackCommand = (AttackCommandEntity)SnapshotCommand(subCommand.AttackCommand),
                        DefendingArmyIds = ConvertToArmyIds(subCommand.Defenders),
                        TargetTileX = subCommand.X,
                        TargetTileY = subCommand.Y
                    };
                }
                else if (command is DefendCommand)
                {
                    snapshot = new DefendCommandEntity();
                }
                else if (command is DeselectArmyCommand)
                {
                    snapshot = new DeselectArmyCommandEntity();
                }
                else if (command is MoveOnceCommand)
                {
                    var subCommand = (MoveOnceCommand)command;
                    snapshot = new MoveCommandEntity()
                    {
                        PathX = ConvertToInts(subCommand.Path, 0),
                        PathY = ConvertToInts(subCommand.Path, 1)
                    };
                }
                else if (command is PrepareForBattleCommand)
                {
                    var subCommand = (PrepareForBattleCommand)command;
                    snapshot = new PrepareForBattleCommandEntity()
                    {
                        DefendingArmyIds = ConvertToArmyIds(subCommand.Defenders),
                        TargetTileX = subCommand.X,
                        TargetTileY = subCommand.Y
                    };
                }
                else if (command is SelectArmyCommand)
                {
                    snapshot = new SelectArmyCommandEntity();
                }
                else if (command is SelectNextArmyCommand)
                {
                    snapshot = new SelectNextCommandEntity();
                }

                // Common properties
                var armyCommand = (ArmyCommand)command;
                var armySnapshot = (ArmyCommandEntity)snapshot;
                armySnapshot.ArmyIds = ConvertToArmyIds(armyCommand.Armies);
            }

            return snapshot;
        }

        private static string[] ConvertToItemNames(List<Artifact> items)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            return items.ConvertAll<string>(a => a.ShortName).ToArray();
        }

        private static int[] ConvertToInts(IList<Tile> path, int dimension)
        {
            if (path == null || path.Count == 0)
            {
                return null;
            }

            int[] pathInts = new int[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                pathInts[i] = (dimension == 0) ? path[i].X : path[i].Y;
            }

            return pathInts;
        }

        private static int[] ConvertToArmyIds(List<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return null;
            }

            return armies.ConvertAll<int>(a => a.Id).ToArray();
        }

        private static int GetPlayerIndex(Player player)
        {
            if (player != null)
            {
                for (int i = 0; i < Game.Current.Players.Count; i++)
                {
                    if (player != null)
                    {
                        if (Game.Current.Players[i].Clan == player.Clan)
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }
    }
}
