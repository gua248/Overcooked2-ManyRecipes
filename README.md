# Overcooked! 2 - Recipe Extension MOD

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) for the game (x86 for Steam, x64 for Epic)
2. Copy `bin/Release/OC2ManyRecipes.dll` to the game's `BepInEx/plugins/` folder

> ### Compiling
>
> You may compile the MOD yourself. The following dependencies need to be copied into `lib/` directory: 
>
> - In the game's `Overcooked2_Data/Managed/` directory `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.IMGUIModule.dll`.
> - In `BepInEx/core/` directory `0Harmony20.dll`, `BepInEx.dll`, `BepInEx.Harmony.dll`.



## How to use

- Requires installation of both host and guest players to work properly.

- Enable in the game main menu Settings - MODs (disabled by default).

- `Many Recipes` option needs to be enabled by both host and guest players.

  Extends recipes for all story, DLC, versus, and horde levels, except story 6-6 and Sun 1-3.

  Compatible with the HardHorde mod.

- `Many Recipes - Display More Menus` option only needs to be enabled by the host, and cannot be enabled alone.

  Increases the number of displayed menus from 2\~5 to 4\~6 (for versus mode from 2\~3 to 3\~4).

- NOT valid in `Arcade Public`.



## Others

- Fix bug: Mid-Autumn moon cakes and Night of the Hangry Horde fruit pies don't show the model in the mixing bowl after finishing baking.
- Fix bug: In hotdog levels, the bun+onion+sauce combo not in a plate doesn't show the model. (when mod enabled)
- Fix bug: In some hotpot levels, certain ingredient combinations could not be put back into the large pot after being ladled out in the small ladle. (when mod enabled)
- Retains the original burger capacity bug, which needs to be utilized to make 3-meat burgers in Surf 2-3.
- Retains a redundant entry in the list of ingredient combinations that can be served on a plate in Surf barbecue levels, i.e., `Mushroom & Pineapple & Beef & Chicken`, which can be served on a plate but does not have a model.