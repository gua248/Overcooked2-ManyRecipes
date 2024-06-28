using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    [BepInPlugin("dev.gua.overcooked.manyrecipes", "Overcooked2 ManyRecipes Plugin", "1.1")]
    [BepInProcess("Overcooked2.exe")]
    public class ManyRecipesPlugin : BaseUnityPlugin
    {
        static ManyRecipesPlugin pluginInstance;
        static Harmony patcher;
        public static List<RecipePatchBase> recipePatches = new List<RecipePatchBase>();

        public void Awake()
        {
            pluginInstance = this;
            patcher = new Harmony("dev.gua.overcooked.manyrecipes");
            
            recipePatches.Add(new SmoothiePatch()); // 00   10
            recipePatches.Add(new KebobPatch());    // 01   16
            recipePatches.Add(new BurgerPatch());   // 02   15
            recipePatches.Add(new BurritoPatch());  // 03   5
            recipePatches.Add(new PizzaPatch());    // 04   8
            recipePatches.Add(new HotPotPatch());   // 05   7
            recipePatches.Add(new RoastPatch());    // 06   9
            recipePatches.Add(new BreakfastPatch());// 07   10
            recipePatches.Add(new SoupPatch());     // 08   4
            recipePatches.Add(new SmorePatch());    // 09   7
            recipePatches.Add(new SushiPatch());    // 10   6
            recipePatches.Add(new FryPatch());      // 11   2
            recipePatches.Add(new FruitsPatch());   // 12   3
            recipePatches.Add(new PastaPatch());    // 13   8
            recipePatches.Add(new SaladPatch());    // 14   3
            recipePatches.Add(new CocoaPatch());    // 15   4
            recipePatches.Add(new HotdogPatch());   // 16   3
            recipePatches.Add(new FloatPatch());    // 17   3
            recipePatches.Add(new SteamedPatch());  // 18   6
            recipePatches.Add(new MDPatch());       // 19   9
            recipePatches.Add(new CakePatch());     // 20   15
                                                    //      153

            Patch.PatchAll(patcher);
            foreach (var patch in recipePatches)
                patcher.PatchAll(patch.GetType());
            foreach (var patched in patcher.GetPatchedMethods())
                Log("Patched: " + patched.FullDescription());
        }

        public static void Log(string msg) { pluginInstance.Logger.LogInfo(msg); }
    }
}