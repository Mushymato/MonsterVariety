import argparse
from collections import defaultdict
import json
import os
from pathlib import Path

OUTPUT = ".output"
OUTPUT_DATA = ".output/data"


def make_include_edits(monster, normal, dangerous):
    modId_monster = f"{{{{ModId}}}}_{monster}"
    targets = [f"{modId_monster}/{x}" for x in normal]
    dangerous_targets = [f"{modId_monster}_dangerous/{x}" for x in dangerous]
    include_changes = []
    editdata = {
        "Action": "EditData",
        "Target": "mushymato.MonsterVariety/Data",
        "Entries": {
            modId_monster: {
                "Id": modId_monster,
                "MonsterName": monster,
            },
        },
    }
    if targets:
        include_changes.append(
            {
                "Action": "Load",
                "Target": ",".join(targets),
                "FromFile": f"Textures/{monster}/{{{{TargetWithoutPath}}}}.png",
            }
        )
        editdata["Entries"][modId_monster]["Varieties"] = {
            target: {"Sprite": target} for target in targets
        }
        editdata["Entries"][modId_monster]["Varieties"]["Default"] = {
            "Sprite": f"Characters/Monsters/{monster}"
        }
    if dangerous_targets:
        include_changes.append(
            {
                "Action": "Load",
                "Target": ",".join(dangerous_targets),
                "FromFile": f"Textures/{monster} Dangerous/{{{{TargetWithoutPath}}}}.png",
            }
        )
        editdata["Entries"][modId_monster]["DangerousVarieties"] = {
            target: {"Sprite": target} for target in dangerous_targets
        }
        editdata["Entries"][modId_monster]["DangerousVarieties"]["Default"] = {
            "Sprite": f"Characters/Monsters/{monster}_dangerous",
        }
    include_changes.append(editdata)
    return include_changes


def make_cp_edits(at_path):
    print(at_path)
    at_root = Path(at_path)
    textures = at_root / "Textures"

    includes = defaultdict(dict)

    for root, _dirs, files in textures.walk():
        texture_n = []
        for filename in files:
            if filename.startswith("texture_"):
                texture_n.append(filename.replace(".png", ""))
        if not texture_n:
            continue
        include_name = root.name
        if include_name.endswith(" Dangerous"):
            includes[include_name.replace(" Dangerous", "")][
                "DangerousVarieties"
            ] = texture_n
        else:
            includes[include_name]["Varieties"] = texture_n

    os.makedirs(str(at_root), exist_ok=True)
    os.makedirs(str(at_root / "data"), exist_ok=True)
    content_changes = []
    for key, data in includes.items():
        with open(at_root / "data" / f"{key}.json", "w") as fn:
            json.dump(
                {
                    "Changes": make_include_edits(
                        key,
                        data.get("Varieties", []),
                        data.get("DangerousVarieties", []),
                    )
                },
                fn,
                indent=2,
            )
        content_changes.append(
            {
                "Action": "Include",
                "FromFile": f"data/{key}.json",
            },
        )
    with open(at_root / "content.json", "w") as fn:
        json.dump({"Format": "2.5.0", "Changes": content_changes}, fn, indent=2)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="do AT to CP stuff")
    parser.add_argument("at_path")
    args = parser.parse_args()

    make_cp_edits(args.at_path)
