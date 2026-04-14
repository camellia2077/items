# Devtools

This directory contains repository-specific development utilities that do not belong to build, deploy, release, or log handling.

## Tools

- `check_naming.py`
  Checks project C# code against repository-specific naming rules.
  Default usage:

  ```powershell
  python .\tools\devtools\check_naming.py --verbose
  ```

  Rules live in:

  - [naming_rules.json](/C:/code/random_gun/tools/devtools/naming_rules.json)

  The first rule set focuses on ETG runtime terminology that should stay aligned with the project glossary:
  - `CharacterSelectSceneName`
  - `LegacyCharacterSelectSceneName`
  - `ReturnToCharacterSelect`
  - `BeginReturnToCharacterSelect`
  - `IsInCharacterSelectHub`
  - `IsCharacterSelectScene`
  - `EnteredCharacterSelectHub`
  - `ReturningToCharacterSelect`

- `loc_scanner/`
  Standalone line-count and size scanning utility with its own internal structure.
