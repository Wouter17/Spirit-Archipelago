# Spirit Island Archipelago

This is an Archipelago implementation for the game [Spirit Island](Archipelago.MultiClient.Net.dll).

In this implementation you can decide yourself what adversaries you want to face with what spirits and difficulty.

**Important:** Because of the large amount of Spirits and Adversaries, by default no goal is set. This means if you use the defealt settings your game will instantly complete! Be sure to set the `Victory Goals` field in your yaml.

_This implementation is largely based on [this](https://github.com/toasterparty/oc2-modding) Archipelago implementation for OC2_

# How To Install (the client)

1. Download and extract the [Latest Release](https://github.com/Wouter17/Spirit-Archipelago/releases).

2. Double click `si-modding-install.bat` and use the file picker window to select your game's .exe file.

3. Run the game once, wait until you reach the main menu, and then close it.

# How To Generate a Spirit Island Archipelago game
_This setup guide assumes you are running Archipelago from source (python files instead of .exe)_

1. Download the latest APWorld from the [releases page](https://github.com/Wouter17/Spirit-Archipelago/releases).
   
2. Copy the apworld file to the `custom_worlds` folder in your Archipelago installation. 
   
3. Run `WebHost.py` which will host the archipelago website on `localhost`.
   
4. Open `localhost` in your browser and navigate to the options page for Spirit Island (should be [http://localhost/games/Spirit%20Island/player-options](http://localhost/games/Spirit%20Island/player-options)).

5. Select your victory goal(s) and also set other options to your liking.
-   For a singleplayer game you can now click generate and you're ready to go!
-   For multiplayer continue reading

6. (multiplayer) Add the `.yaml` that was downloaded to the `players` folder, along with any other yamls from players that should be added to the game.

7. (multiplayer) Either run `Generate.py` or press the Generate button on the launcher to generate a seed.

8. (multiplayer) When this has completed, you will have a zip file in your Archipelago/Output folder.

9. (multiplayer) Host the output zip either using the Host option on the launcher or by uploading it to Archipelago.gg

# How To Build (read only if you are a developer)

1. Install latest [.NET sdk](https://dotnet.microsoft.com/en-us/)

2. Copy the following DLLs to `\lib\`:

```
SpiritIslandEngine.dll
```

3. Build

Run

```
dotnet build
```

4. Install [BepInEx 5.4.23.4](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.4) (x64 version) in your Spirit Island folder

5. Add the build output dlls `Archipelago.dll` and `Archipelago.MultiClient.Net.dll` to `/BepInEx/plugins`