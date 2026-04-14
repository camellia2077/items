# Release Tools

This folder contains player-facing release packaging logic.

Files:

- `build_release_package.py`: main release zip entrypoint
- `release_package_builder.py`: staging and zip assembly
- `release_package_compliance.py`: package content checks and notices
- `release_package_upstream.py`: upstream archive download and verification helpers
- `release_package_metadata.json`: packaging metadata

Typical usage:

- `python .\tools\build_release_package.py --configuration Release`
