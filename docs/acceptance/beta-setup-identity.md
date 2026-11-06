# Beta setup identity acceptance

## Acceptance

- Valid campaign setup shows no floating “Ready.” status and keeps Start enabled.
- Invalid setup shows a readable, anchored validation error and disables Start;
  correcting the setup clears the error and enables Start again.
- Setup validation and status layout are covered at 1024x768, 1280x720,
  1920x1080, and 2560x1080 ultrawide.
- Horse Lords and Selentines use the same default clan palette in setup and
  gameplay. Mod-defined colors and roster behavior remain data-driven.

## Classic palette evidence

The army flag sprites show Horse Lords' light blue as the flat pixel color
`(0, 134, 199)` in `WismUnity/Assets/Sprites/Armies/Horse Lords/horselords_flag_1.png`
and the other Horse Lords flags. Selentines flags use `(0, 85, 199)` in
`WismUnity/Assets/Sprites/Armies/Selentines/selentines_flag_1.png` and the other
Selentines flags. Selentines unit sprites, including `selentines_cavalry.png`
and `selentines_wizards.png`, include the flat yellow `(199, 199, 0)`. These
sprite pixels support the described light-blue and yellow/blue identities; the
values were read from the assets rather than chosen from a new palette.

| Clan | Field | Before | After |
|---|---|---|---|
| Horse Lords | Primary | `(134, 199, 0)` | `(0, 134, 199)` |
| Horse Lords | Secondary | `(0, 0, 0)` | unchanged |
| Selentines | Primary | `(255, 0, 0)` | `(199, 199, 0)` |
| Selentines | Secondary | `(0, 85, 119)` | `(0, 85, 199)` |

Only these entries change in the shared `Clan.json` and its two synchronized
Unity copies. `GameSetup` reads `ClanInfo` colors for clan labels. Gameplay
identity also comes from that same shared data: army overlays use
`ClanInfo.PrimaryColor`, ownership visuals use the same primary color, and
strategic reports capture it from the clan info. No sprite or runtime tinting
code is modified.

## Validation status

`BetaSetupIdentityJourneyTests` adds two focused tests: one compares the three
shared palette files against the sprite-supported values, and one exercises
valid/invalid setup feedback and both clan label colors at all four viewports.
The Unity PlayMode tests are **UNRUN** in this remote environment; no Unity
editor executable is available here, so native Unity validation remains
required. Run the fixture from the Unity 6000 editor with:

```sh
Unity -batchmode -nographics -projectPath WismUnity -runTests -testPlatform PlayMode -testFilter BetaSetupIdentityJourneyTests -testResults TestResults/beta-setup-identity.xml
```

The fixture has 2 test cases, with the setup journey exercising 4 viewports.
Existing `GameSetupPointerMatrixTests` continue to cover paging, long names,
roles, input, world changes, and Mods/Load/Start journeys.
