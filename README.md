# BlesoniteClient

BlesoniteClient is a modification of thundagun that allows for pushing unity connector packet data into blender using IPC methods

Future features:
* A mode where Blender will record session as an animation for post processing rendering with EEVEE or Cycles or any other rendering engine inside blender
* Faster integration so python doesn't freeze up (Shared memory?)
* Normals, Materials, Textures, Shaders?
* Security possibly

**Warning: This mod is experimental. THIS ONLY WORKS IN LOCAL DUE TO PERFORMANCE ISSUES. THE GAME WILL NOT BOOT UNLESS INSTRUCTIONS ARE FOLLOWED. USE WITH CARE!**

## Installation

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [Thundagun.dll](https://github.com/989onan/BlesoniteClient/releases/latest/download/Thundagun.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create this folder for you.
3. (Recommended) Download and place [ResoniteModSettings.dll](https://github.com/badhaloninja/ResoniteModSettings/releases/latest/download/ResoniteModSettings.dll) into your `rml_mods` folder. This will allow you to configure the mod in-game.
4. Start Resonite. If you want to verify that the mod is working you can check your Resonite logs.

## IMPORTANT STEP
5. Install pywin32 for your blender install you are opening this mod with. The Python exe should be in your blender install under <VERSION>/python/python.exe Then run a cmd there and type in the cmd "python.exe -m pip install pywin32"

## Usage
1. load the blender file, and go to scripting, reload the python file for booting the server
2. compile the mod, the dll goes into your resonite install under rml_mods
3. make sure you have pywin32 installed inside blender
3. run the python script inside blender. the game should boot from blender in screen mode.
4. Wait a long time for the slots to initalize
5. Blender should not be populated. closing Resonite will automatically clear the scene and purge all resonite associated data.


## Contributions and Support

- **Issues**: Report bugs or request features via the repository's Issues section.
- **Pull Requests**: Submit code contributions through Pull Requests.

