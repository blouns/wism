# Clan Rosters

GameSetup reads available clans from the installed `Clan.json` after applying
selected flavor overlays. `ShortName` is the stable identity used by cities,
players and saved games. Changing a display name does not create a new clan.

```json
{
  "ShortName": "SilverGuard",
  "DisplayName": "Silver Guard",
  "PrimaryColor": "(180, 200, 220)",
  "SecondaryColor": "(0, 0, 0)",
  "Playable": true,
  "VisualClanName": "Sirians",
  "StartingGold": 1000
}
```

`Playable` defaults to true, including for legacy files. Neutral is never offered
as a player. Set `Playable` to false to keep a clan's data installed without
offering it in new-game setup. Supply a starting city in the world's `City.json`
for every playable clan. Start remains disabled if a selected clan has no city.

`PrimaryColor` and `SecondaryColor` color the foreground and shadow of the clan
name. Use integer RGB triples between 0 and 255. Legacy `Color` is accepted when
`PrimaryColor` is absent. Names are literal text, not rich-text markup.

There is no eight-clan setup cap. Up to eight clans use the original rows; larger
rosters are paged. Choices and difficulty follow `ShortName` through paging,
reordering and the Mods round trip. All selected pages contribute to the game.

New clans need artwork. `VisualClanName` explicitly reuses an installed clan's
army, flag and city art without sharing ownership or player identity. Alias
chains are invalid. Reused art keeps its original palette; label colors do not
recolor sprites. Omit the alias only when the Unity scene supplies art for the new
clan ID. Use the exact installed artwork ID. Additional terrain modifiers and
world production remain separate mod data, not inferred from the art alias.

Existing flavor overlays can change names, colors and playable membership:

```json
{
  "clans": [
    {
      "shortName": "SilverGuard",
      "displayName": "Silver Guard",
      "primaryColor": "(30, 180, 90)",
      "secondaryColor": "(0, 0, 0)",
      "playable": false
    }
  ]
}
```

Overlays reference existing stable IDs. Add new clan definitions to the base mod,
not to a flavor rename. Removing an overlay restores base names, colors and
membership. A saved game's mod fingerprint still governs save compatibility.

See [Settings UI Matrix](../testing/settings-ui-matrix.md) for repeatable checks.
