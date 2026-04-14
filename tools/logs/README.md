# Log Tools

This folder contains log filtering and triage helpers for `BepInEx\LogOutput.log`.

Files:

- `extract_randomloadout_log.py`: extracts only `RandomLoadout`-owned lines
- `read_log.py`: filters logs by preset or regex, supports tailing and consecutive-line de-duplication

Typical usage:

- `python .\tools\read_log.py --preset bossrush --preset error --tail 200 --dedupe-consecutive --ignore-case`
- `python .\tools\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log"`
