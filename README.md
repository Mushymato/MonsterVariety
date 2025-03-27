# MonsterVariety

Framework mod, allow modders to reskin monsters.

Made this because AT is failing to cover a few edge-cases.

Note: `[CP] Visual Monster Variety` lacks the actual assets since they don't belong to me. Please see mod of same name on nexus.

## Model

Target `mushymato.MonsterVariety/Data` and add an entry like this:

```json
// The Key/Id should be unique for your mod, to achieve compatibility.
"{{ModId}}_Armored Bug": {
  "Id": "{{ModId}}_Armored Bug",
  // Internal name of the monster, mandatory.
  // If you aren't sure about the name, look for "Try ApplyMonsterVariety on <monster name>" in the trace logs.
  // It's possible for other mods to change this name.
  "MonsterName": "Armored Bug",
  "Varieties": {
    "{{ModId}}_Armored Bug/texture_0": {
      // Load the sprite to this target
      "Sprite": "{{ModId}}_Armored Bug/texture_0",
      // Optional fields
      "Condition": null, // Game State Query
      "Season": null, // Current season, respects the location
      "Precedence": 0, // Order to check in, lower is earlier
      "ExtraDrops": {
        // extra drop items, these are item queries with Condition https://stardewvalleywiki.com/Modding:Item_queries
        "{{ModId}}_ExtraMeat1": {
          "Id": "{{ModId}}_ExtraMeat1",
          "ItemId": "(O)684"
        }
      }
    },
    // This entry is the vanilla appearance, it's treated the samesame as any other variety.
    // You do not have to include it if you wish to completely override this monster's sprites.
    "Default": {
      "Sprite": "Characters/Monsters/Armored Bug"
    }
    // add more varieties as desired
  },
  // shared extra drop, applies to all monsters with this name
  "SharedExtraDrops": {
    // shared extra drop items, these are item queries with Condition https://stardewvalleywiki.com/Modding:Item_queries
   "{{ModId}}_ExtraMeat2": {
      "Id": "{{ModId}}_ExtraMeat2",
      "ItemId": "(O)684"
    }
  },
  // same as Varieties, but for dangerous monsters
  "DangerousVarieties": {
    "{{ModId}}_Armored Bug_dangerous/texture_0": {
        "Sprite": "{{ModId}}_Armored Bug_dangerous/texture_0"
    },
  },
  // like SharedExtraDrops but for dangerous monsters
  "DangerousSharedExtraDrops": {
   "{{ModId}}_ExtraMeat3": {
      "Id": "{{ModId}}_ExtraMeat3",
      "ItemId": "(O)684"
    }
  },
}
```

`mushymato.MonsterVariety/Data` is actually a list, two mods adding varieties to the same monster will appear as 2 different entries so as long as they use unique id. These entries will be merged before used to check what variants should apply.