# Development

This page covers local development workflows for `RandomLoadout`.

## Build

PowerShell wrapper:

```powershell
python .\tools\build.py --configuration Debug
```

Python entrypoint:

```powershell
python .\tools\build.py --configuration Debug
```

Release build:

```powershell
python .\tools\build.py --configuration Release
```

## Test

PowerShell wrapper:

```powershell
python .\tools\test.py --configuration Debug
```

Python entrypoint:

```powershell
python .\tools\test.py --configuration Debug
```

## Tooling

The scripts use the system `.NET Framework` MSBuild at:

- `C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe`

Build logic and test logic now live in Python tools under [`tools/`](../../tools/).
The PowerShell files are thin wrappers that forward arguments to Python.

## Local Dependencies

Required local DLLs are described in:

- [`../../lib/README.md`](../../lib/README.md)

## IDE

The repository includes:

- `RandomLoadout.sln`

for IDE-based work.
