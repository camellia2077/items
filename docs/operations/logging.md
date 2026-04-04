# Logging

This page covers the logging conventions and log helper scripts used by `RandomLoadout`.

## Message Prefixes

RandomLoadout-written log lines use structured prefixes such as:

- `[RandomLoadout][Init]`
- `[RandomLoadout][Run]`
- `[RandomLoadout][Grant]`
- `[RandomLoadout][Command]`

These prefixes make it easier to distinguish plugin messages from Unity, ETG, BepInEx, and other runtime noise.

## Extract RandomLoadout Lines

Print only RandomLoadout-owned lines from a BepInEx log file:

```powershell
python .\scripts\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log"
```

Write the filtered output to a separate file:

```powershell
python .\scripts\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" -o ".\randomloadout.log"
```

Include older unprefixed plugin lines as well:

```powershell
python .\scripts\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --include-unprefixed-plugin-lines
```

## Notes

- missing log files now use the same friendly error output style as the other Python scripts
- command, run-state, grant, and startup logs are intentionally separated by prefix for easier triage
