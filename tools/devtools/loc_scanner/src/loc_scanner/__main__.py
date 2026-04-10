from .cli_app import LocCliApplication


def main() -> int:
    return LocCliApplication().run()


if __name__ == "__main__":
    raise SystemExit(main())
