The mod seems to do nothing, what do I check?

Check that `[General] Enable Mod` and `[Crops] Enable` are `true`, that KydlCore 1.2.0 or newer is installed, and that you restarted the game after changing a setting: the items, the skill and the sounds are added when the game starts. Then set `[DebugLog] Enable` to `true` and look in `BepInEx/LogOutput.log`; it lists the asset bundle contents, every crop with its stats, recipe and station, and the sound setup.
