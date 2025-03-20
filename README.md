# MonsterVariety

Framework mod, allow modders to reskin monsters.

Made this because AT is failing to cover a few edge-cases.

Note: `[CP] Visual Monster Variety` lacks the actual assets since they don't belong to me. Please see mod of same name on nexus.

## Model

Target `mushymato.MonsterVariety/Data` and add an entry like this:

```json
// The key should be unique for your mod, to achieve compatibility.
// MonsterName is the internal name of the monster, this field is mandatory.
// If you aren't sure about the name, look for 'Try ApplyMonsterVariety on ' in the trace logs.
// It's possible for other mods to change this name.
"{{ModId}}_Armored Bug": {
  "Id": "{{ModId}}_Armored Bug",
  "MonsterName": "Armored Bug",
  "Varieties": {
    "{{ModId}}_Armored Bug/texture_0": {
      // Load the sprite to this target
      "Sprite": "{{ModId}}_Armored Bug/texture_0",
      // Optional fields
      "Condition": null, // Game State Query
      "Season": null, // Current season, respects the location
      "Precedence": 0, // Order to check in, lower is earlier
    },
    // This entry is the vanilla appearance, it's optional if you don't want to include it in the mix
    "Default": {
      "Sprite": "Characters/Monsters/Armored Bug"
    }
    // add more varieties as desired
  },
  // same as Varieties, but for dangerous monsters
  "DangerousVarieties": {
    "{{ModId}}_Armored Bug_dangerous/texture_0": {
        "Sprite": "{{ModId}}_Armored Bug_dangerous/texture_0"
    },
  }
}
```

`mushymato.MonsterVariety/Data` is actually a list, two mods adding varieties to the same monster will appear as 2 different entries so as long as they use unique id. These entries will be merged before used to check what variants should apply.