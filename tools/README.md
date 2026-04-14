# Tools

This directory keeps repository tooling grouped by purpose.

## Categories

- `build/`: build and test entrypoints
- `deploy/`: deploy-to-game tooling
- `logs/`: log extraction and filtering helpers
- `release/`: player-facing release packaging
- `docs/`: documentation generation
- `devtools/`: standalone development utilities with their own structure

## 30-Second Commands

- Build debug:
  `python .\tools\build\build.py --configuration Debug`
- Run tests:
  `python .\tools\build\test.py --configuration Debug`
- Deploy release to game:
  `python .\tools\deploy\deploy_mod.py "<game path>" --configuration Release --overwrite-config`
- Read recent Boss Rush + error logs:
  `python .\tools\logs\read_log.py --preset bossrush --preset error --tail 200 --dedupe-consecutive --ignore-case`
- `python .\tools\devtools\check_naming.py --verbose`
  Run the repository-specific naming checker for `src/**/*.cs`.

## Read Next

- Build and environment details:
  [`../docs/getting-started/development-setup.md`](../docs/getting-started/development-setup.md)
- Deploy workflow:
  [`../docs/operations/deploy.md`](../docs/operations/deploy.md)
- Logging workflow:
  [`../docs/operations/logging.md`](../docs/operations/logging.md)
