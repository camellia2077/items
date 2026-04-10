from pathlib import Path
import sys

SRC_ROOT = Path(__file__).resolve().parent / "src"
if str(SRC_ROOT) not in sys.path:
    sys.path.insert(0, str(SRC_ROOT))

from loc_scanner.__main__ import main


if __name__ == "__main__":
    raise SystemExit(main())
