# Save Compatibility

Save schema versions are independent of game release versions. New saves carry a
`SaveFormat` header with format `WISM.Save`, `SchemaVersion`,
`MinimumReaderVersion`, and a diagnostic `ProducerVersion`.

The current reader supports schema 1 and unversioned legacy saves (version 0).
Legacy normalization adds the header and missing AI display preferences in memory;
it does not reconstruct absent campaign data or modify the original file. Existing
world and mod compatibility checks still apply. Old content identifiers may require
the original content pack; format compatibility alone cannot replace missing assets.

Newer readers must retain supported migrations and regression fixtures. An older
reader rejects a newer schema, even if its declared minimum reader is lower: future
schema semantics have not been proven safe. Optional additions within schema 1 must
not change existing field meanings or require an older reader to understand them.
Changes requiring new semantics must increment the schema. Forward compatibility
means a defined safe boundary and actionable refusal, not a promise that every old
binary can interpret every future save. Pre-versioning binaries cannot enforce this
contract retroactively.

Loading never upgrades a file on disk. A subsequent explicit Save writes the current
format using atomic replacement. Keep an original copy when testing old campaigns.
Malformed headers, duplicate JSON fields, unsupported schemas and incompatible
content are rejected before changing scenes or applying the save. Deep reconstruction
failure continues to use transactional rollback for same-scene loads.
