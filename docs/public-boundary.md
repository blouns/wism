# Public Boundary

This repository is public. It should be understandable and useful without exposing private automation plans, private repositories, internal codenames, credentials, or unlicensed assets.

## Tiers

### Tier 0: Public Product Surface

Material intended for everyone:

- README content
- screenshots and release notes
- public architecture summaries
- build and test instructions
- public issue templates
- public gameplay docs

Tier 0 must use polished language and avoid private implementation details.

### Tier 1: Public Contributor Surface

Material intended for public contributors and coding agents working in this repo:

- contributor guidance
- public MCP/editor tool docs
- public TODO and backlog items
- public validation commands
- public architecture notes

Tier 1 may discuss implementation constraints, but only for this public repo.

### Tier 2: Private Only

Material that must not be committed here:

- private repository names
- private autonomy or orchestration plans
- internal codenames not already public
- private telemetry strategy
- private eval policy
- secrets, tokens, keys, local credentials, or personal account data
- private customer, company, or operator context
- copied commercial assets or documents without redistribution rights

Use a private repository or local notes for Tier 2 material.

## Practical Checks

Before committing public docs or tools, search for:

```powershell
rg -n "private|secret|token|password|internal|codename|credential" README.md docs WismUnity WismClient
```

Also scan any new docs for accidental references to private repos, private agent systems, or internal labels.

## Public Tooling Rule

WISMUnity custom MCP tools in this repo should be narrow, explicit, and safe by default.

Tool categories:

- `Read-only`: inspect project, scene, world, or game state.
- `Runtime command`: execute game commands through WismClient rules.
- `Mutation-capable`: modify scenes, assets, prefabs, MOD JSON, or project settings.

Mutation-capable tools require backup, preview/dry-run, explicit approval, and validation.
