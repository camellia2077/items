from pathlib import Path
import sys


if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    from loc_scanner.cli_app import LocCliApplication
else:  # pragma: no cover
    from .cli_app import LocCliApplication


def main() -> int:
    return LocCliApplication().run()


if __name__ == "__main__":
    raise SystemExit(main())
