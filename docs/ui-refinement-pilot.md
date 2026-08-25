# UI Refinement Pilot

WISM keeps the original game's interaction semantics, information density, and visual grammar while making controls more forgiving and deterministic. The pilot remains on uGUI and covers army selection and attack routing, owned-cities production, and GameSetup.

## Runtime Contracts

- `WismUiSurface` identifies a stable surface and its required states.
- `WismUiControl` identifies semantic role, action, state, and overlap priority.
- `WismHitArea` separates visible bounds from effective input bounds.
- `WismMotionProfile` caps short feedback and transition timing and supports reduced motion.
- `WismTypographyProfile` applies an explicit approved font asset and deterministic sizes.
- `WismUiInputAdapter` keeps legacy mouse behavior authoritative while allowing the Input System to supply pointer state when enabled.

Desktop controls have a 32 logical-unit minimum. Effective touch-ready targets are at least 44 by 44 without enlarging classic artwork. Overlap resolution is deterministic: enabled state, explicit priority, distance to visible bounds, then hierarchy order.

Decorative text, cursor graphics, and minimap markers do not receive raycasts. Map actions are rejected while the EventSystem owns the pointer.

## Unity Pipeline Commands

The public Unity Pipeline extension exposes:

- `wism_ui_inventory`: semantic controls, required states, geometry, fonts, overflow, and raycast targets.
- `wism_ui_exercise`: mouse, keyboard, and simulated-touch semantic action traces.
- `wism_ui_capture`: a Play Mode screenshot and geometry manifest under ignored `Library/WismUiCaptures`.
- `wism_ui_compare`: pixel drift plus paired geometry-manifest locations for the latest two captures.

These commands emit runtime evidence only. Screenshots and baseline decisions are not committed to the public repository.

## Tests

The PlayMode suite covers pointer-bound sweeps, overlap arbitration, disabled and rejected actions, overlay-to-map suppression, semantic IDs, reduced motion, typography defaults, owned-city production states, and GameSetup behavior. Performance measurement warms the resolver and records 100 interactions.

Unity Code Coverage, Performance Testing, and Input System package versions are locked in `Packages/manifest.json` and `Packages/packages-lock.json`.
