"""Public current-source module boundary entry point (Linux/WSL/CI)."""

import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent / "Boundaries"))
from runner import main


if __name__ == "__main__":
    sys.exit(main())
