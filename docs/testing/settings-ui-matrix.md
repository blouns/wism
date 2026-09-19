# Settings UI Matrix

Run this on demand after setup, input routing, save/load, world selection or UI
package changes. It is not required on every commit.

```text
unity test <isolated-project>/WismUnity --mode PlayMode --filter GameSetupPointerMatrixTests --output <proof>/settings.xml --timeout 900 -- -force-d3d11 -wism-mute-audio
```

Use a hidden process launcher on Windows. Batch tests assert that their process
owns zero visible windows and use synthetic input devices, never desktop input.
The full suite needs offscreen graphics for world/minimap rendering. Null-graphics
mode is suitable only for the settings controls subset, not world startup.

## Contracts

- Actual authored-scene raycasts and press/release dispatch reach the intended
  Selectable. Transparent expanded targets never replace click handlers.
- All 256 clan subsets preserve exact clan identity and independent role state;
  fewer than two players cannot start.
- All eight warrior icons cycle Human, Knight, Baron, Lord, Warlord, Human.
  Role labels remain alternative targets. Adjacent settings never change a role.
- Each role has a distinct transparent 32x30 portrait, point-filtered without
  compression or mipmaps. Portrait, label and explicit difficulty mapping agree
  for pointer and keyboard input, wrapping and clan deselection/reselection.
  Changing portraits preserves the existing button geometry. World starts check
  the selected portraits against actual player tiers, not just setup labels.
- Random start is off and disabled; Interactive UI is on and disabled. Show AI
  combat is independently selectable and feeds the created/loaded game.
- Every world option is checked. Worlds absent from build settings cannot start.
- Two, four and eight-player Illuria starts and the other included worlds verify
  actual player identities, AI tiers and neutral capitals for unselected clans.
- Pointer sweeps use 1024x768, 1280x800 and 1920x1080 without changing canvas modes
  or flattening authored geometry.
- Mods navigates once and returns. Load disables empty slots, cancels back to
  setup, and restores the saved roster and world rather than the setup selection.
- Shipped menu cursor settings use the system pointer and its native hotspot;
  testing the gameplay overlay's calculation helpers alone is insufficient.
- Mini-Illuria uses the same packaged mod root validated by setup, never the
  legacy editor-only scene path. All included worlds fit minimap geometry after
  the queued game creation completes; UI, camera texture, collider and map agree.
- A rendered Mini-Illuria minimap capture checks that the target is not blank.

Run the neighboring `GameSetupModSettingsFlowTests`, `JoyfulFidelityUiTests`,
`ModSettingsUiTests` and army UI journeys when shared hit-area code changes.

Synthetic input and geometry checks are not screenshot fidelity approval or a
native player test. Final Windows player qualification belongs on an isolated
interactive test desktop; human gameplay acceptance remains a separate gate.
