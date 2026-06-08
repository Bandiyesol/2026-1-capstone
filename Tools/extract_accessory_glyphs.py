import os
import re
import sys

FOLDER = os.path.join(os.path.dirname(__file__), "..", "Assets", "Data", "Accessory")


def decode_unicode_escapes(text: str) -> str:
    return re.sub(r"\\u([0-9A-Fa-f]{4})", lambda m: chr(int(m.group(1), 16)), text)


def main() -> int:
    chars: set[str] = set()
    for name in sorted(os.listdir(FOLDER)):
        if not name.endswith(".asset"):
            continue
        path = os.path.join(FOLDER, name)
        raw = open(path, encoding="utf-8").read()
        text = decode_unicode_escapes(raw)
        for key in ("displayName", "description", "accessoryType"):
            match = re.search(rf"{key}:\s*(.+)", text)
            if not match:
                continue
            value = match.group(1).strip().strip('"')
            for ch in value:
                if ord(ch) > 127:
                    chars.add(ch)

    ordered = sorted(chars, key=lambda c: ord(c))
    print(f"Unique Korean/extra chars: {len(ordered)}")
    print("".join(ordered))

    missing_report = "끼납낡럭꽃냉"
    for ch in missing_report:
        print(f"{ch} U+{ord(ch):04X} in set: {ch in chars}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
