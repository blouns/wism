# Beta Ending Branches

This page lists the campaign endings WISM supports for Beta, what the WismClient rules engine guarantees for each one, which automated tests cover it, and what still needs a hands-on check in WismUnity.

The executable regression suite for these branches is `WismClient/Wism.Client.Test/Unit/BetaEndingBranchesTests.cs`, with turn-order and offer-integrity cases in `BetaEndingIntegrityTests.cs` (the same fixture). It plays the real eight-clan Illuria campaign (80 cities: one home city per clan and 72 neutral cities). Turns change only through the normal start-turn and end-turn commands. Clan-to-clan captures use the normal capture command. The victory evaluator only ever reads live world and player state.

## Ending Rules

### Conquest

- A clan with no cities is eliminated at the start of its own next turn. Its field armies and heroes leave the map with it. Until then it is still in the war, even with zero cities.
- The campaign ends at the end of a turn when only the clan whose turn it was is still in the war. That clan is the winner. In practice this is the winner's turn after the last opponent's elimination.
- Neutral cities neither block conquest nor change hands because of it. They stay neutral, and the result reports their share as unclaimed.
- If every clan is eliminated, the campaign ends without inventing a winner.

### Turn Order Across Eliminated Seats

- Looking up the next player visits each seat at most once, in seat order, wrapping around the roster. It never changes whose turn it is.
- Any number of consecutive eliminated seats are skipped, including across the end of the roster.
- When every other seat is eliminated, the next player is the current player if still alive. When every seat is eliminated, there is no next player.
- Looking up the next player without a roster is an error.
- The looked-up seat is the seat the real end turn selects.

### Defeat

- WismClient has no separate defeat result. A defeated human is eliminated like any other clan. Their seat is skipped for the rest of the campaign.
- While two or more clans remain, the campaign continues without the defeated human. When one computer clan is left, the result is a conquest with that clan as winner.
- The defeated human stays on the roster as a human seat. Presentation therefore treats the campaign as a human campaign that another clan won, not as an observer-only match.

### Hotseat

- With several human seats, eliminating one human does not end the campaign. Turns pass directly between the surviving seats.
- The last clan standing wins by conquest, as in a single-human campaign.
- Hotseat campaigns never receive the classic surrender offer, even when only one human seat survives.

### Classic Surrender Offer

The offer applies to a campaign with exactly one human seat against any number of computer clans. The number of clans varies per map, so it is not part of the rule. Every one of the following must hold:

| Condition | Threshold |
| --- | --- |
| Human seats on the roster | exactly 1, still in the war |
| Computer clans still in the war (not yet eliminated) | at least 1 |
| Cities on the map, including neutral cities | at least 10 (smaller maps never receive the offer) |
| Cities owned by the human | more than 50% of the map's cities (on Illuria's 80: more than 40, so 41 or more) |
| Human's lead over the strongest computer clan | more than 18.75% of the map's cities (on Illuria: more than 15, so 16 or more; exactly 15 is not enough) |

The Warlords manual states the rule for Illuria: the surviving computer players offer surrender when the human has more than 40 cities and the most powerful computer player is not within 15 cities. WISM keeps those exact boundaries on the 80-city map and scales both by each map's own city count. For example, a 40-city map needs 21 cities and a lead of 8, and a 160-city map needs 81 cities and a lead of 31.

- Neutral cities count toward the realm total and the unclaimed share. They never count toward the human or any computer clan.
- Eliminated computer clans do not close the offer. The lead is measured against the strongest computer clan still in the war.
- A computer clan that has just lost its last city is still in the war until its own next turn start eliminates it.
- AI-only campaigns never receive an offer, however dominant one computer clan becomes.
- Evaluating the offer has no side effects. WismUnity evaluates it after a human ends a turn, and only while no result has been recorded yet. If the offer is eligible, WismUnity records it as the campaign's pending result. It then pauses the game until the human decides, before any computer turn runs.

### Accepting or Rejecting the Offer

- **Accept:** every city, including neutral cities, and every army on the board pass to the human. The campaign ends in inspection mode, and turns, production and computer play stop.
- **Reject:** the war continues with ownership unchanged. The recorded rejection keeps the offer closed for the rest of the campaign, even if the human's lead later grows. The evaluator itself keeps no memory; the stored rejection is what closes the offer. The recorded rejection cannot be turned into an acceptance later, including after a save and load.
- A conquest after a rejection replaces the rejection with a normal conquest result.
- Only the campaign's current pending offer can be accepted or rejected. A copy of an offer kept from earlier is refused once it has been rejected, accepted, or replaced by a newer offer, once the campaign has ended, and when it comes from a different or newly started game. A refused decision changes nothing: no city, army, recorded result or game state moves.
- Accepting also requires the offer's winner to be a single surviving clan on the campaign's roster. Every check runs before the first ownership change.

### Save and Load

A pending offer, a rejected offer, an inspection-mode ending and a conquest ending all survive save and load. This holds for the WismUnity save format and the terminal and agent save format. A pending offer loaded from a save can still be accepted or rejected. A copy of the offer held from before the load belongs to the earlier game state and is refused; front ends read the loaded campaign's pending result instead.

### After an Ending

After any ending, turn commands fail and the classic computer AI produces no orders. No turn counter, city, army or treasury changes. This also holds after a reload, and while a computer seat holds the turn when the campaign ends. Loading or starting a different game is the only way out of a finished campaign.

## Intended Rule vs Current Behavior

| Topic | Intended rule | Current behavior | Status |
| --- | --- | --- | --- |
| Who offers surrender | In a one-human campaign, the *surviving* computer clans offer surrender, whatever the map's clan count. | Any one-human campaign with at least one computer clan still in the war. Eliminated computer clans do not close the offer. | Fixed. |
| Map-relative thresholds | Thresholds follow each map's size. | Both thresholds are shares of the map's cities (the Illuria values scaled by city count). Maps under 10 cities never offer. | Fixed. Per-mod tuning is not implemented. |
| Rejected surrender | The remaining computer clans fight on as a desperate opposing bloc. | The rejection is recorded and the war continues. Computer strategy does not change. | Not yet implemented. |
| Pending offer pause | The human decides before play continues. | WismUnity pauses play. The rules engine itself does not block turns while an offer is pending, and the terminal and agent runtimes do not present the offer. | By design. Front ends own the pause. |
| Offer validation | Only the campaign's current pending offer can be accepted or rejected. | Accept and reject refuse any offer that is not the campaign's current pending result, and any decision after the campaign has ended, before changing anything. | Fixed. |
| Automated sessions | n/a | A non-interactive WismUnity session accepts a pending offer automatically. | Confirm this is intended only for automation. |
| Console turn handoff | The next living seat starts its turn. | The next-player lookup skips every eliminated seat and always returns. | Fixed. |

## Branch Coverage

`BetaEndingBranchesTests` is abbreviated **BEB** in the table below.

| Branch | Rules engine tests | WismUnity PlayMode tests |
| --- | --- | --- |
| Offer at 41 cities with a 16-city lead | BEB `ClassicSurrender_Illuria_41Cities_16CityLead_IsOffered`, `..._42Cities_16CityLead_IsOffered`; `PlaygroundScenarioRunnerTests.ClassicSurrender_RequiresSingleHumanFortyOneCitiesAndRunawayLead` | — |
| No offer at a lead of exactly 15 | BEB `..._41Cities_Exactly15CityLead_IsNotOffered`, `..._42Cities_Exactly15CityLead_IsNotOffered` | — |
| No offer at 40 cities | BEB `..._40Cities_HugeLead_IsNotOffered`; `PlaygroundScenarioRunnerTests.ClassicSurrender_DoesNotTriggerAtFortyCitiesOrCloseComputer` | — |
| Neutral cities count toward the total only | BEB boundary cases (unclaimed share and neutral count) | — |
| Computer that just lost its last city still counts | BEB `ClassicSurrender_AtHumanEndTurn_StillCountsAComputerThatJustLostItsLastCity` | — |
| Thresholds scale with the map's city count; no offer under 10 cities | BEB `ClassicSurrender_*CityMap_*` | — |
| Offer after computer eliminations and on maps with any number of computer clans | BEB `ClassicSurrender_IsStillOfferedAfterAComputerClanIsEliminated`, `ClassicSurrender_*ClanMap_*` | — |
| No offer in AI-only or hotseat campaigns | BEB `ClassicSurrender_IsNeverOfferedInAiOnlyCampaign_EvenToADominantComputer`, `Hotseat_...`; `PlaygroundScenarioRunnerTests.ClassicSurrender_DoesNotTriggerForMultiHumanOrAiOnlyGames` | — |
| Pending offer survives save and load, then is accepted | BEB `PendingOffer_SurvivesUnitySaveLoad_...`, `PendingOffer_SurvivesTerminalSaveLoad_...` | `CampaignPresentation_LoadedOfferResumesWithoutQueuedEndTurn` (reject path) |
| Accept: whole realm, inspection mode, play stops | BEB `PendingOffer_*`; `TerminalCampaignTests.AcceptSurrender_TransfersNeutralDefendersAndPreservesArmiesThroughLoad`, `AcceptedSurrenderIdentity_SurvivesSerialization` | `CampaignPresentation_AcceptTransfersRealmThenFreezesForInspection` |
| Reject: war continues, offer stays closed, cannot be accepted later | BEB `RejectedOffer_SurvivesUnitySaveLoad_...`, `RejectedOffer_SurvivesTerminalSaveLoad_...`; `PlaygroundScenarioRunnerTests.ClassicSurrender_RejectRecordsOutcomeAndContinuesGame` | `CampaignPresentation_RejectKeepsOwnershipAndDoesNotReoffer` |
| Rejection followed by human conquest | BEB `RejectedOffer_LaterHumanConquestReplacesOutcome_AndLeavesNeutralCitiesNeutral` | — |
| Lead measured against the strongest computer, wherever it sits | BEB `ClassicSurrender_LeadIsMeasuredAgainstTheStrongestComputer_*` | — |
| Only the current pending offer can be decided | BEB `RetainedOffer_AfterRejection_CannotBeAcceptedOrRejectedAgain`, `ReplacedOffer_CannotBeDecided_OnlyTheCurrentPendingOfferCan`, `OfferFromAnEarlierGame_IsRefusedByANewGame_BeforeAnyMutation`, `AcceptedOffer_IsTerminal_RetainedOfferCannotReopenIt` | — |
| Pending offer after save and load, under both save formats | BEB `PendingOffer_AfterUnitySaveLoad_OnlyTheLoadedOfferCanBeDecided`, `PendingOffer_AfterTerminalSaveLoad_OnlyTheLoadedOfferCanBeDecided` | — |
| Next player skips eliminated seats and wraps around | BEB `NextPlayer_SkipsSeveralConsecutiveEliminatedSeats_AndMatchesTheRealEndTurn`, `NextPlayer_FromLastSeat_*`, `NextPlayer_WrapsAcross*`, `NextPlayer_WhenOnlyTheCurrentSeatIsAlive_*`, `NextPlayer_WhenEverySeatIsEliminated_*`, `NextPlayer_WithoutARoster_*` | — |
| Human conquest | BEB `RejectedOffer_LaterHumanConquest...`, `Hotseat_...`; `TerminalCampaignTests.Conquest_IsLatchedAndRepeatedTurnsCannotMutateCampaign(True)` | `CampaignVictory_HumanRemainsTerminal` |
| Human defeat by a computer clan | BEB `HumanDefeat_EliminatedHumanIsSkipped_AndLastComputerStandingWinsByConquest` | `CampaignPresentation_EliminatedHumanIsNotAnAiOnlyCampaign` |
| Hotseat elimination and surviving humans | BEB `Hotseat_EliminatedHumanIsSkipped_SurvivingHumansPlayOn_AndLastHumanWins`; `TerminalCampaignTests.TwoLivingHotseatPlayers_DoNotEndCampaign` | — |
| AI-only conquest with neutral cities remaining | `EightClanEndgameTests` (both tests) | `CampaignVictory_AiOnlyRemainsTerminal`, `CampaignPresentation_AllAiUsesOnlyPersistentResult` |
| No survivors | `TerminalCampaignTests.NoSurvivors_EndsWithoutInvalidPlayerIndexOrInventedWinner` | `CampaignPresentation_UnknownWinnerDoesNotInventAVictor` |
| No further turns or AI orders after an ending (including after reload) | BEB `PendingOffer_*`, `HumanDefeat_...`, `Hotseat_...`, `RejectedOffer_LaterHumanConquest...`, `AcceptedOffer_IsTerminal_...` (classic computer AI); `TerminalCampaignTests.StaleCommandsAndAi_DoNotExecuteAfterGameOver`; `EightClanEndgameTests.EightClanCampaign_GameOverLatchesWinnerAgainstStaleEndTurns` | `CampaignVictory_*` |
| Conquest survives save and load | BEB `RejectedOffer_LaterHumanConquest...`, `HumanDefeat_...`; `TerminalCampaignTests.TerminalSave_RoundTripsOutcomeAndPlayableSaveRestoresNormalTurns` | `CampaignVictory_InspectionSaveAndLoadKeyboardRemainAvailable` |

## WismUnity Checks Still Needed

The PlayMode tests above inject the offer or drive elimination through the rules engine directly. Check these by hand in WismUnity before Beta sign-off:

1. **Offer raised by real play.** In a one-human Illuria campaign, end a turn holding 41 or more cities with a lead of 16 or more. The peace offer (arrival, scroll, choice) must appear before any computer turn runs. It must not appear at a lead of exactly 15.
2. **Offer after an elimination.** Eliminate one computer clan, then reach the thresholds. The offer must still appear.
3. **Rejection holds across sessions.** Reject, save, quit, load, and grow the lead further. The offer must never return. Computer turns resume immediately after "Continue War".
4. **Accept after load.** Save while the offer is on screen, load, and accept. All 80 cities show the winner's colors, the victory, dominion and inspection pages appear, and map and city inspection still work while end turn, movement and production are unavailable.
5. **Human defeat in play.** Lose the last city. The "For you, the war is over" notice appears. The defeated seat can no longer act, and the game does not wait for input from it. Computer turns run to the end, and the final message names the winning clan.
6. **Hotseat handoff.** With three human seats, eliminate one. The next prompt goes to the next surviving human. When the last human wins, the victory pages name that player.
7. **Observer campaigns.** In an all-computer campaign, the ending notice appears, the human victory pages are skipped, the realm stays inspectable, and no further computer turns run.
8. **Automated sessions.** Confirm that automatic acceptance of a pending offer happens only in non-interactive sessions.

## Running the Suite

From `WismClient/`:

```powershell
dotnet test Wism.Client.Test\Wism.Client.Test.csproj -c Release --filter "FullyQualifiedName~BetaEndingBranchesTests"
```

The test project, like the rules engine's mod loader and trace log, uses Windows path conventions. Run it on Windows.
