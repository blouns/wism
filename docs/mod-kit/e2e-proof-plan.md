# Mod Kit E2E Proof Plan

This plan defines the proof standard for Mod Kit phases. A phase is not Green
because code exists or a process launched. A phase is Green only when the full
flow works end to end and leaves durable evidence that can be reviewed later.

## No Watermelon Rule

Do not mark a Mod Kit phase Green unless all applicable layers are verified:

- WismClient validation and focused tests.
- Command-line or sidecar author workflow.
- AgentPlayground smoke or campaign execution.
- Actual WismUnity verification.
- Proof artifacts with paths, timestamps, inputs, and outcomes.
- Clean handling of generated files, copied Unity projects, and plugin DLLs.

If Unity verification is blocked, stale, launch-only, or missing proof
artifacts, the phase is Yellow or Red. It is not Green.

## Unity Proof Standard

Unity proof must be actual Unity execution or actual Unity observation. A
non-Unity build, a CLI validation pass, or a launched process is not enough.

Acceptable Unity evidence includes:

- A Unity batchmode import or smoke run against an isolated project copy.
- A live-editor read-only report from the current project when explicitly safe.
- A Unity Playground smoke manifest that records the selected profile, packs,
  world, scene state, and result.
- Console-log evidence showing no relevant Unity errors.

Preferred proof bundle layout:

```text
artifacts/mod-kit/<work-item-id>/<run-id>/
  proof-summary.json
  commands.txt
  wismclient-test.log
  cli-report.json
  agentplayground-report.json
  unity-smoke-manifest.json
  unity-console.log
```

Minimum Unity manifest fields:

- `status`
- `startedAtUtc`
- `endedAtUtc`
- `unityVersion`
- `projectPath`
- `profileId`
- `enabledPackIds`
- `worldName`
- `sceneName`
- `gameInitialized`
- `dirtyScenes`
- `errors`
- `warnings`
- `proofNotes`

For mutation-capable workflows, the proof bundle must also include:

- Preview report.
- Backup path.
- Changed file list.
- Revalidation result.
- Recovery instructions.

## Phase Gates

### M1: Data-Pack Foundation

Green requires:

- Feature-pack templates validate.
- `ModKitValidatorTests` pass.
- `ModularProfileCatalogTests` pass.
- AgentPlayground loads `classic-warlords` with zero packs.
- AgentPlayground loads `classic-warlords` with proof packs.
- Unity Playground smoke records selected profile and packs.
- Unity proof manifest shows no dirty scene changes and no relevant errors.

### M2: Author Feedback And Unity Read-Only Bridge

Green requires:

- `mod-kit validate` runs outside NUnit and returns JSON plus friendly text.
- Invalid packs fail with actionable diagnostics.
- Unity read-only Mod Kit report returns profile, packs, validation, scene
  summary, dirty-scene state, and scene-vs-MOD drift summary.
- Unity report does not save, reset, import, export, or mutate scene objects.
- Sidecar-facing API can consume the same structured report.
- Proof bundle includes CLI report, Unity report, and Unity console evidence.

### M3: Sidecar Pack And World Authoring

Green requires:

- Sidecar opens a mod workspace.
- Sidecar creates a feature pack from templates.
- Sidecar creates a world draft from templates.
- Sidecar runs validation without Unity.
- AgentPlayground loads the generated mod.
- Unity previews the generated mod without saving scenes.
- Any mutation path uses preview, backup, apply, revalidate, and recovery
  evidence.
- Proof bundle includes created files, validation reports, AgentPlayground
  smoke, Unity proof, and changed-file audit.

### M4: Scenario, Asset, And Campaign Modding

Green requires:

- Scenario and campaign manifests validate.
- Asset references validate for existence, importability, and provenance.
- AgentPlayground runs bounded campaign smoke against the mod.
- Unity launches the selected scenario or records a precise unsupported-feature
  report.
- Proof bundle includes campaign outcome, coverage counters, Unity launch
  evidence, and package metadata.

### M5: Stable Mod Ecosystem

Green requires:

- Package install, uninstall, and upgrade validation pass.
- Compatibility matrix covers WismClient, WismUnity, Companion, and
  AgentPlayground.
- Old supported mod packages continue to load or receive migration diagnostics.
- Public examples run through the complete proof harness.
- Release proof includes provenance, license, validation, Unity smoke, and
  AgentPlayground smoke.

## Failure Classification

- Red: broken build, broken validation, Unity crash, data loss risk, invalid
  generated content, or missing required proof for a claimed Green phase.
- Yellow: flow works partially, Unity proof is incomplete, evidence is local-only
  and not reviewable, or generated files need manual cleanup.
- Green: full acceptance criteria met with durable proof artifacts.

## Operator Checklist

Before closing a work item:

- Run focused WismClient tests.
- Run the CLI or sidecar author flow.
- Run AgentPlayground where gameplay/loadability matters.
- Run Unity proof using isolated batchmode when possible.
- Capture proof artifacts under the work item id.
- Inspect generated file churn before staging.
- Record any skipped verification with the reason and the next unblock step.
