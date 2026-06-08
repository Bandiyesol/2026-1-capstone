"""Duplicate WeaponSelectUI hierarchy as RewardSelectUI in ProtoType_LTG.unity."""
import re
from collections import deque
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets/Scenes/ProtoType_LTG.unity"
ROOT_GO = 1641493326
ROOT_RECT = 1641493327
ROOT_WEAPON_UI = 1641493328
CONTENT_PANEL_GO = 623864430
CANVAS_RECT = 461710348
WEAPON_SELECT_SCRIPT = "03e83d1d368f31242acf5af7aaca38a8"
REWARD_SELECT_SCRIPT = "ae269f8f74d068f43ba8a13c038916c2"
ID_OFFSET = 900_000_000

BTN_IDS = [879163206, 1972672413, 853437880]
ICON_IDS = [246519792, 713906292, 853125401]
TITLE_IDS = [1910446549, 1965494919, 2116330943]
DETAIL_IDS = [2144105048, 2014686754, 1024911500]


def main():
    text = SCENE.read_text(encoding="utf-8")
    if "m_Name: RewardSelectUI" in text:
        print("RewardSelectUI already exists — skipping.")
        return

    block_pattern = re.compile(r"--- !u!(?P<type>\d+) &(?P<id>\d+)\n(?P<body>.*?)(?=\n--- !u!|\Z)", re.S)
    blocks = {}
    order = []
    for m in block_pattern.finditer(text):
        fid = int(m.group("id"))
        blocks[fid] = (m.group("type"), m.group("body"))
        order.append(fid)

    ref_pattern = re.compile(r"\{fileID: (\d+)\}")

    def refs(body: str):
        return [int(x) for x in ref_pattern.findall(body)]

    queue = deque([ROOT_GO])
    seen = set()
    while queue:
        fid = queue.popleft()
        if fid in seen or fid not in blocks:
            continue
        seen.add(fid)
        for r in refs(blocks[fid][1]):
            if r in blocks:
                queue.append(r)

    id_map = {old: old + ID_OFFSET for old in seen}

    def remap_body(body: str) -> str:
        def repl(m):
            old = int(m.group(1))
            if old in id_map:
                return f"{{fileID: {id_map[old]}}}"
            return m.group(0)

        out = ref_pattern.sub(repl, body)
        if "OnPickWeapon" in out:
            out = re.sub(
                r"  m_OnClick:\n    m_PersistentCalls:\n      m_Calls:\n      - m_Target:.*?\n        m_CallState: 2\n",
                "  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n",
                out,
                flags=re.S,
            )
        return out

    def build_reward_ui_body(body: str) -> str:
        head_end = body.find("  m_EditorClassIdentifier:")
        if head_end == -1:
            return body
        head = body[:head_end]
        panel = id_map[CONTENT_PANEL_GO]
        lines = [
            head.rstrip(),
            "  m_EditorClassIdentifier: Assembly-CSharp::RewardSelectUI",
            f"  panel: {{fileID: {panel}}}",
            "  slotButtons:",
        ]
        for bid in BTN_IDS:
            lines.append(f"  - {{fileID: {id_map[bid]}}}")
        lines.append("  slotIcons:")
        for iid in ICON_IDS:
            lines.append(f"  - {{fileID: {id_map[iid]}}}")
        lines.append("  slotTitles:")
        for tid in TITLE_IDS:
            lines.append(f"  - {{fileID: {id_map[tid]}}}")
        lines.append("  slotDetails:")
        for did in DETAIL_IDS:
            lines.append(f"  - {{fileID: {id_map[did]}}}")
        return "\n".join(lines) + "\n"

    duplicated = []
    for fid in order:
        if fid not in seen:
            continue
        btype, body = blocks[fid]
        new_id = id_map[fid]
        new_body = remap_body(body)

        if fid == ROOT_GO:
            new_body = new_body.replace("m_Name: WeaponSelectUI", "m_Name: RewardSelectUI")
        if fid == ROOT_WEAPON_UI:
            new_body = build_reward_ui_body(new_body.replace(WEAPON_SELECT_SCRIPT, REWARD_SELECT_SCRIPT))

        duplicated.append(f"--- !u!{btype} &{new_id}\n{new_body}")

    new_root_rect = id_map[ROOT_RECT]
    canvas_needle = f"  - {{fileID: {ROOT_RECT}}}\n"
    canvas_insert = canvas_needle + f"  - {{fileID: {new_root_rect}}}\n"
    if canvas_insert not in text:
        if canvas_needle not in text:
            raise SystemExit("Canvas child list anchor not found")
        text = text.replace(canvas_needle, canvas_insert, 1)

    anchor = "--- !u!222 &1641493330"
    insert_at = text.find(anchor)
    if insert_at == -1:
        raise SystemExit("Insert anchor not found")
    end = text.find("\n--- !u!", insert_at + 1)
    if end == -1:
        end = len(text)

    text = text[:end] + "\n" + "\n".join(duplicated) + "\n" + text[end:]
    SCENE.write_text(text, encoding="utf-8")
    print(f"Inserted RewardSelectUI ({len(seen)} objects). New root rect: {new_root_rect}")


if __name__ == "__main__":
    main()
