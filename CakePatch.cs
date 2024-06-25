using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class CakePatch : RecipePatchBase
    {
        static CakePatch instance;
        static int[] prefabIndex;

        public CakePatch() { instance = this; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientIngredientContainer), "StartSynchronising")]
        public static void SetCapacity(Component __instance)
        {
            if (!ManyRecipesSettings.enabled ||
                __instance.GetComponent<MixableContainer>() == null ||
                __instance.GetComponent<CookableContainer>() == null)
                return;
            LevelConfigBase levelConfig = GameUtils.GetLevelConfig();
            if (levelConfig == null) return;
            var oldEntries = levelConfig.GetAllRecipes();
            bool[] hasEntry = oldRecipeNames.Select(name => oldEntries.Find(x => x.name == name) != null).ToArray();
            if (!hasEntry.Any(x => x)) return;

            __instance.GetComponent<IngredientContainer>().m_capacity = 4;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetPrefabForNode")]
        public static void OrderToPrefabLookupGetPrefabForNodePatch(OrderToPrefabLookup __instance, AssembledDefinitionNode _node, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || prefabIndex == null || __result != null) return;
            var simpleNode = _node.Simpilfy();
            if (__instance.name == "FryableObjectsLookup" || 
                __instance.name == "DLC02_FryableObjectsLookup" ||
                __instance.name == "DLC05_FryableObjectsLookup" ||
                __instance.name == "DLC09_FryableObjectsLookup" || 
                __instance.name == "DLC08_FryerObjectLookup")
            {
                for (int i = 0; i < prefabIndex.Length; i++)
                {
                    CookedCompositeOrderNode recipe = instance.newRecipes[i] as CookedCompositeOrderNode;
                    CookedCompositeOrderNode oldRecipe = instance.oldRecipes[prefabIndex[i]] as CookedCompositeOrderNode;
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, recipe.m_composition[0].Simpilfy()))
                    {
                        __result = __instance.GetPrefabForNode(oldRecipe.m_composition[0].Simpilfy());
                        return;
                    }
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, recipe.Simpilfy()))
                    {
                        __result = __instance.GetPrefabForNode(oldRecipe.Simpilfy());
                        return;
                    }
                }
            }
            if (__instance.name == "PancakeCosmeticPrefabs" || 
                __instance.name == "DLC02_PancakeCosmeticPrefabs" ||
                __instance.name == "DLC05_PancakeCosmeticPrefabs" || 
                __instance.name == "DLC09_CakePrefabLookup")
            {
                for (int i = 0; i < prefabIndex.Length; i++)
                {
                    CookedCompositeOrderNode recipe = instance.newRecipes[i] as CookedCompositeOrderNode;
                    CookedCompositeOrderNode oldRecipe = instance.oldRecipes[prefabIndex[i]] as CookedCompositeOrderNode;
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, recipe.Simpilfy()))
                    {
                        __result = __instance.GetPrefabForNode(oldRecipe.Simpilfy());
                        return;
                    }
                }
            }

            // for mixers' CookableContainer.m_approvedContentsList
            // for frying pans, this is used to check if contents can be placed in pans
            // but seems unused for mixers' CookableContainer
            // (the oven's can-cook check is in ServerCookingStation.CanCook)
            //if (__instance.name == "CakePrefabLookup" || 
            //    __instance.name == "DLC03_CakePrefabLookup" ||
            //    __instance.name == "DLC09_MixerPrefabLookup")
            //{
            //    for (int i = 0; i < prefabIndex.Length; i++)
            //    {
            //        CookedCompositeOrderNode recipe = instance.newRecipes[i] as CookedCompositeOrderNode;
            //        CookedCompositeOrderNode oldRecipe = instance.oldRecipes[prefabIndex[i]] as CookedCompositeOrderNode;
            //        if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, recipe.m_composition[0].Simpilfy()))
            //        {
            //            __result = __instance.GetPrefabForNode(oldRecipe.m_composition[0].Simpilfy());
            //            return;
            //        }
            //    }
            //}
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComboOrderToPrefabLookup), "GetPrefabForNode")]
        public static void ComboOrderToPrefabLookupGetPrefabForNodePatch(ComboOrderToPrefabLookup __instance, AssembledDefinitionNode _node, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || prefabIndex == null || __result != null) return;
            var simpleNode = _node.Simpilfy();
            if (__instance.name == "CakeCosmeticPrefabs" ||
                __instance.name == "DLC03_CakeCosmeticPrefabs" ||
                __instance.name == "DLC09_CakeCosmeticPrefabs")
            {
                if (instance.oldRecipes[15] != null)
                    foreach (int i in new int[] { 15, 16, 17, 18, 19 })
                        if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, instance.oldRecipes[i].Simpilfy()))
                        {
                            __result = instance.oldRecipes[i].m_platingPrefab;
                            return;
                        }
                if (instance.oldRecipes[20] != null)
                    foreach (int i in new int[] { 20, 21, 22, 23 })
                        if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, instance.oldRecipes[i].Simpilfy()))
                        {
                            __result = instance.oldRecipes[i].m_platingPrefab;
                            return;
                        }
                for (int i = 0; i < prefabIndex.Length; i++)
                {
                    CookedCompositeOrderNode recipe = instance.newRecipes[i] as CookedCompositeOrderNode;
                    CookedCompositeOrderNode oldRecipe = instance.oldRecipes[prefabIndex[i]] as CookedCompositeOrderNode;
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, recipe.m_composition[0].Simpilfy()))
                    {
                        __result = __instance.GetPrefabForNode(oldRecipe.m_composition[0].Simpilfy());
                        return;
                    }
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, recipe.Simpilfy()))
                    {
                        __result = __instance.GetPrefabForNode(oldRecipe.Simpilfy());
                        return;
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerFlowControllerBase), "StartSynchronising")]
        [HarmonyPatch(typeof(ClientFlowControllerBase), "StartSynchronising")]
        public static void PrepareNewRecipes(IFlowController __instance)
        {
            if (__instance is ClientFlowControllerBase clientFlowControllerBase &&
                clientFlowControllerBase.GetComponent<ServerFlowControllerBase>() != null) return;
            if (instance == null) return;
            instance.newRecipes = null;
            instance.entries = null;
            instance.oldRecipes = null;
            prefabIndex = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            bool[] hasEntry = oldRecipeNames.Select(name => oldEntries.Find(x => x.name == name) != null).ToArray();
            if (!hasEntry.Any(x => x)) return;

            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            if (hasEntry[0] && hasEntry[1] && !hasEntry[10])
                newRecipeDataTmp.Add(newRecipeData[0]);
            if (hasEntry[2] && hasEntry[3])
                newRecipeDataTmp.Add(newRecipeData[1]);
            if (hasEntry[5])
                newRecipeDataTmp.Add(newRecipeData[2]);
            if (hasEntry[1] && hasEntry[6])
                newRecipeDataTmp.Add(newRecipeData[3]);
            if (hasEntry[0] && hasEntry[6])
            {
                newRecipeDataTmp.Add(newRecipeData[4]);
                newRecipeDataTmp.Add(newRecipeData[5]);
            }
            if (hasEntry[10])
                if(!hasEntry[0])
                    newRecipeDataTmp.Add(newRecipeData[6]);
                else
                {
                    newRecipeDataTmp.Add(newRecipeData[7]);
                    newRecipeDataTmp.Add(newRecipeData[8]);
                }
            if (hasEntry[15])
                newRecipeDataTmp.Add(newRecipeData[9]);
            if (hasEntry[13])
                if (!hasEntry[14])
                    newRecipeDataTmp.Add(newRecipeData[10]);
                else
                {
                    newRecipeDataTmp.Add(newRecipeData[13]);
                    if (hasEntry[12])
                    {
                        newRecipeDataTmp.Add(newRecipeData[11]);
                        newRecipeDataTmp.Add(newRecipeData[12]);
                    }
                }
            if (hasEntry[20])
                newRecipeDataTmp.Add(newRecipeData[14]);
            if (hasEntry[24])
                if (!hasEntry[7])
                    newRecipeDataTmp.Add(newRecipeData[15]);
                else
                {
                    newRecipeDataTmp.Add(newRecipeData[16]);
                    newRecipeDataTmp.Add(newRecipeData[17]);
                    newRecipeDataTmp.Add(newRecipeData[18]);
                }

            IngredientOrderNode[] ingredients = ingredientNames.Select(x => (
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x || r.name == "DLC09_" + x || r.name == "DLC13_" + x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )) as IngredientOrderNode).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();

            prefabIndex = new int[newRecipeDataTmp.Count];
            instance.newRecipes = new CookedCompositeOrderNode[newRecipeDataTmp.Count];
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                MixedCompositeOrderNode mix = ScriptableObject.CreateInstance<MixedCompositeOrderNode>();
                mix.name = "Mixed_" + newRecipeDataTmp[i].name;
                mix.m_uID = newRecipeDataTmp[i].id + newRecipeData.Length;
                mix.m_composition = newRecipeDataTmp[i].ingredients.Select(x => ingredients[x]).ToArray();
                mix.m_progress = MixedCompositeOrderNode.MixingProgress.Mixed;

                prefabIndex[i] = newRecipeDataTmp[i].prefab;
                CookedCompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CookedCompositeOrderNode>();
                CookedCompositeOrderNode prefab = instance.oldRecipes[newRecipeDataTmp[i].prefab] as CookedCompositeOrderNode;
                newRecipe.name = newRecipeDataTmp[i].name;
                newRecipe.m_uID = newRecipeDataTmp[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = new OrderDefinitionNode[] { mix };
                newRecipe.m_cookingStep = prefab.m_cookingStep;
                newRecipe.m_progress = CookedCompositeOrderNode.CookingProgress.Cooked;

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                gui0.m_children = new List<int> { 1 };
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = newRecipeDataTmp[i].ingredients.Select(x => ingredients[x].m_iconSprite).ToList();
                gui1.m_tileDefinition.m_modifierPictures = prefab.m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                newRecipe.m_orderGuiDescription = new RecipeWidgetUIController.RecipeTileData[] { gui0, gui1 };

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                int score = newRecipeDataTmp[i].score;
                if (levelConfig.name.StartsWith("Resort_2_4")) score = 140;
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.newRecipes[i],
                    m_scoreForMeal = score
                };
                entries.Add(entry);
            }
            if (levelConfig.name.StartsWith("Courtyard_2_2"))
                entries.Add(new RecipeList.Entry
                {
                    m_order = instance.oldRecipes[18],
                    m_scoreForMeal = 140
                });
            instance.entries = entries.ToArray();
        }

        // old              new
        // 0,1              0
        // 2,3,4            1
        // 0,1,2,3,4        0,1
        // 1,5              2
        // 1,6              3
        // 0,6,2,4          4,5
        // 10,11            6
        // 24,25            15
        // 0,1,10,11        7,8
        // 7,8,9,24,25      16,17,18
        // 15,16,17,(18),19 9
        // 13               10
        // 12,13,14         11,12,13
        // 13,14            13
        // 20,21,22,23      14
        // 20,22,23         14

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8882000, name="Pancake_Chocolate2",             ingredients=new int[]{0,1,3,3}, prefab=1, score=120 },
            new RecipeData { id=8882001, name="Cake_Plain2",                    ingredients=new int[]{0,1,2,2}, prefab=2, score=120 },
            new RecipeData { id=8882002, name="Pancake_ChocolateStrawberry",    ingredients=new int[]{0,1,3,5}, prefab=5, score=120 },
            new RecipeData { id=8882003, name="Pancake_ChocolateBlueberry",     ingredients=new int[]{0,1,3,6}, prefab=1, score=120 },
            new RecipeData { id=8882004, name="Cake_Blueberry",                 ingredients=new int[]{0,1,2,6}, prefab=2, score=140 },
            new RecipeData { id=8882005, name="Pancake_Blueberry2",             ingredients=new int[]{0,1,6,6}, prefab=6, score=140 },
            new RecipeData { id=8882006, name="ChristmasPuddingOnlyOrange",     ingredients=new int[]{0,1,8}, prefab=11, score=100 },
            new RecipeData { id=8882007, name="Pancake_ChocolateOrange",        ingredients=new int[]{0,1,3,8}, prefab=1, score=120 },
            new RecipeData { id=8882008, name="ChristmasPuddingWithChocolate",  ingredients=new int[]{0,1,7,3}, prefab=11, score=120 },
            new RecipeData { id=8882009, name="FruitPie_BlackberryCherry",      ingredients=new int[]{0,1,13,14}, prefab=16, score=140 },
            new RecipeData { id=8882010, name="Donut_Raspberry2",               ingredients=new int[]{0,1,10,10}, prefab=13, score=120 },
            new RecipeData { id=8882011, name="Donut_HoneyRaspberry",           ingredients=new int[]{0,1,9,10}, prefab=13, score=120 },
            new RecipeData { id=8882012, name="Donut_HoneyChocolate",           ingredients=new int[]{0,1,9,11}, prefab=14, score=120 },
            new RecipeData { id=8882013, name="Donut_RaspberryChocolate",       ingredients=new int[]{0,1,10,11}, prefab=14, score=120 },
            new RecipeData { id=8882014, name="DLC13_MoonPie_Chocolate2",       ingredients=new int[]{0,1,17,17}, prefab=22, score=80 },
            new RecipeData { id=8882015, name="DLC09_ChristmasPuddingOnlyOrange",     ingredients=new int[]{0,1,8}, prefab=25, score=100 },
            new RecipeData { id=8882016, name="DLC09_Pancake_ChocolateStrawberry",    ingredients=new int[]{0,1,3,5}, prefab=9, score=120 },
            new RecipeData { id=8882017, name="DLC09_Pancake_ChocolateOrange",        ingredients=new int[]{0,1,3,8}, prefab=8, score=120 },
            new RecipeData { id=8882018, name="DLC09_ChristmasPuddingWithChocolate",  ingredients=new int[]{0,1,7,3}, prefab=25, score=120 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "Pancake_Plain",
            "Pancake_Chocolate",            // 1
            "Cake_Plain",
            "Cake_Chocolate",
            "Cake_Carrot",                  // 4
            "StrawberryPancake",
            "BlueberryPancake",             // 6
            "DLC09_Pancake_Plain",
            "DLC09_Pancake_Chocolate",
            "DLC09_Pancake_Strawberry",     // 9
            "ChristmasPudding",
            "ChristmasPuddingWithOrange",   // 11
            "Donut_Plain",
            "Donut_Raspberry",
            "Donut_Chocolate",              // 14
            "FruitPie_Apple",
            "FruitPie_Blackberry",
            "FruitPie_Cherry",
            "FruitPie_AppleCherry",
            "FruitPie_AppleBlackberry",     // 19
            "DLC13_MoonPie_Strawberry",
            "DLC13_MoonPie_Watermelon",
            "DLC13_MoonPie_Chocolate",
            "DLC13_MoonPie_ChocolateStrawberry",    // 23
            "DLC09_ChristmasPudding",
            "DLC09_ChristmasPuddingWithOrange",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Flour",
            "Egg",
            "Honeycomb",
            "Chocolate",
            "Carrot",
            "PancakeStrawberry",
            "Blueberry",
            "DriedFruit",
            "Orange",
            "Honeycomb",    // (Donut)
            "Raspberry",
            "Chocolate",    // (Donut)
            "Apple",
            "Blackberry",
            "Cherry",
            "DLC13_Strawberry",
            "DLC13_Melon",
            "DLC13_Chocolate",
        };
    }
}
