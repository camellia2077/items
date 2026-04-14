# Start Here

This is the handoff entry for a new programmer or agent.

If your change touches ETG runtime behavior, do not start by editing code.

## Required Reading

Read these first, in order:

1. [Terminology And Naming](../reference/terminology.md)
2. [System Overview](../architecture/system-overview.md)
3. [Runtime Hotspots](../architecture/runtime-hotspots.md)
4. [Testing Matrix](../reference/testing-matrix.md)
5. [Source Guide](../../src/AGENTS.md)

## Task Paths

If you are changing Boss Rush:

1. [Terminology And Naming](../reference/terminology.md)
2. [Runtime Hotspots](../architecture/runtime-hotspots.md)
3. [ModTheGungeonAPI Reference](../reference/modthegungeonapi.md)
4. [Commands](../reference/commands.md)
5. [Smoke Checklist](../operations/smoke-checklist.md)

If you are changing character-select-hub behavior:

1. [Terminology And Naming](../reference/terminology.md)
2. [Character Switch Strategy](../decisions/character-switch-strategy.md)
3. [Runtime Hotspots](../architecture/runtime-hotspots.md)
4. [Source Guide](../../src/AGENTS.md)

If you are changing command UI, item grant, or pickup lookup:

1. [Commands](../reference/commands.md)
2. [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)
3. [Pickups](../reference/pickups.md)
4. [ModTheGungeonAPI Reference](../reference/modthegungeonapi.md)

If you are changing build, deploy, or workflow tooling:

1. [Development Setup](./development-setup.md)
2. [Tools README](../../tools/README.md)
3. [Deploy](../operations/deploy.md)
4. [Logging](../operations/logging.md)

## Minimum Checks Before Editing

- Confirm the gameplay term and the scene token are not being mixed.
- Confirm whether the target area is listed in [Runtime Hotspots](../architecture/runtime-hotspots.md).
- Confirm whether the change can stay inside `RandomLoadout.Core` instead of ETG runtime code.

## Minimum Checks After Editing

- Run [Testing Matrix](../reference/testing-matrix.md) items that match the change.
- Read the BepInEx log for runtime-facing changes.
- Update the matching doc if behavior, terminology, or workflow changed.
