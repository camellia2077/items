from __future__ import annotations

import runpy
from pathlib import Path


if __name__ == "__main__":
    runpy.run_path(str(Path(__file__).resolve().parent / "docs" / "generate_pickup_docs.py"), run_name="__main__")
