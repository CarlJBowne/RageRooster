Hello, Developers! Here is information regarding the Rage Rooster Unity project root ("Assets"):
(NOTE: Files labeled "_README" are for developers to get an overview regarding the utility of a given folder. Please read them when you see them. Thank you!)

- "_NewAssets" is intended for any new assets introduced to the project. This is a temporary destination until the import pipeline is implemented. Please add all new assets to a new folder in this location. i.e. if you have made a chair with textures, please create a folder _NewAssets/Chair and place all associated files (.fbx, .png, etc.) files inside.
- "_Unsorted" contains assets not yet fully migrated to the updated project layout due to impementation inconsistencies or other reasons.
- "Actors" contains assets intended for use in-game. They "act" and "do game things" if that makes sense.
- "Art" contains most other art assets.
- "Audio" contains audio-related assets and prefabs.
- "Data" contains data. Not super well organized yet but also less often accessed.
- "Misc" contains other files so as not to clutter up the workspace. Should be reserved for non-essential or junk content.
- "Plugins" contains plugins. Astonishing.
- "Resources" is a special Unity folder name to allow for certain runtime loading. For use by technical people who know when and why to use it.
- "Scenes" contains scenes, including levels and utility scenes. This will likely be adjusted at some point in the future once more systems are adjusted.
- "Settings" contains various settings.
- "Testing" contains test files. Ideally should be moved to "Misc" but want to avoid potential breakages.
- "Tooling" contains tooling-related files. These aren't plugins because that would require skill.
- "UI" contains UI-related files. It's a bit disorganized at the moment.