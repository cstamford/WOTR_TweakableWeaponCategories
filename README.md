# Tweakable Weapon Categories

This mod allows you to tweak weapon categories and subcategories. It's purpose is to modify which weapons are considered monk and finessable weapons.

Now you can fulfill your dream of dual wielding finessable monk scimitars, just like your idol Drizzt.

Please note that this mod has only been tested with the monk and finessable weapon options. Other options may not work in some or all situations.

# How to install

0. Download the latest published zip file.
1. Install [UnityModManager](https://www.nexusmods.com/site/mods/21)
2. Install the zip with UnityModManager.

# How to use

Ctrl + F10 to access the mod menu. Then simply navigate to the weapon category and tweak its subcategories as desired. Remember to re-equip items after changing their categories to see the updated effects.

# How to Compile

0. Install all required development pre-requisites:
	- [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/downloads/)
	- [.NET "Current" x86 SDK](https://dotnet.microsoft.com/download/visual-studio-sdks)
1. Download and install [Unity Mod Manager (UMM)](https://www.nexusmods.com/site/mods/21)
2. Execute UMM, Select Pathfinder: WoTR, and Install
3. Create the environment variable *WrathInstallDir* and point it to your Pathfinder: WoTR game home folder
	- tip: search for "edit the system environment variables" on windows search bar45. Use "Install Release" or "Install Debug" to have the Mod installed directly to your Game Mods folder
4. Run [AssemblyPublicizer](https://github.com/CabbageCrow/AssemblyPublicizer) on the WotR Assembly-CSharp.dll inside the WotR folder you set earlier.
5. Build! If you get assembly reference errors, check the project file and make sure your publicized assembly is in the correct location.

# Links

Source code: https://github.com/cstamford/WOTR_TweakableWeaponCategories
Nexus: https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/74/

# Credits

Thanks to [ThyWoof's mod template](https://github.com/ThyWoof/PathfinderWoTRModTemplate).
