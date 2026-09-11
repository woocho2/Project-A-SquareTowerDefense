"""Print selected Canvas text names with their parent hierarchy for UI wiring."""

import re
from pathlib import Path


SOURCE = Path(__file__).resolve().parents[1] / "Assets/2. Prefab/Canvas.prefab"
TARGET_NAMES = (
    "Txt_Tier", "Txt_Color", "Txt_Emblem", "Txt_Power", "Txt_PowerValue",
    "Txt_Range", "Txt_RangeValue", "Txt_Action", "Txt_ActionValue",
    "Txt_AtteckCount", "Txt_AtteckCountValue", "Txt_CriticalRate",
    "Txt_CriticalRateValue", "Txt_CriticalDamage", "Txt_CriticalDamageValue",
    "Txt_Gold", "Txt_Gem", "Txt_Wave", "Txt_GameSpeedName",
    "Txt_CreateTowerValue", "Txt_ColorUpTowerValue", "Txt_TierUpTowerValue",
)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")
    objects: dict[str, str] = {}
    transforms: dict[str, tuple[str, str | None]] = {}

    for chunk in re.split(r"(?=--- !u!\d+ &)", text):
        game_object = re.match(r"--- !u!1 &(\d+)\nGameObject:(.*)", chunk, re.S)
        if game_object:
            name = re.search(r"\n  m_Name: (.*)", game_object.group(2))
            component = re.search(r"component: \{fileID: (\d+)\}", game_object.group(2))
            if name and component:
                objects[game_object.group(1)] = name.group(1)
                transforms[component.group(1)] = (game_object.group(1), None)

        transform = re.match(r"--- !u!224 &(\d+)\nRectTransform:(.*)", chunk, re.S)
        if transform:
            game_object = re.search(r"m_GameObject: \{fileID: (\d+)\}", transform.group(2))
            parent = re.search(r"m_Father: \{fileID: (\d+)\}", transform.group(2))
            if game_object:
                transforms[transform.group(1)] = (
                    game_object.group(1), parent.group(1) if parent else None
                )

    transform_by_object = {object_id: transform_id for transform_id, (object_id, _) in transforms.items()}
    for target_name in TARGET_NAMES:
        for object_id, object_name in objects.items():
            if object_name != target_name:
                continue
            transform_id = transform_by_object.get(object_id)
            chain: list[str] = []
            while transform_id:
                current_object_id, parent_transform_id = transforms[transform_id]
                chain.append(objects.get(current_object_id, "<unnamed>"))
                transform_id = parent_transform_id
            print(f"{target_name}: {' <- '.join(chain[:5])}")


if __name__ == "__main__":
    main()
