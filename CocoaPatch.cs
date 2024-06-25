using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class CocoaPatch : RecipePatchBase
    {
        static CocoaPatch instance;
        static IngredientOrderNode[] allIngredients;

        public CocoaPatch() { instance = this; }

        static FieldInfo fieldInfo_m_lookupArray = AccessTools.Field(typeof(ComboOrderToPrefabLookup), "m_lookupArray");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComboOrderToPrefabLookup), "FindPrefab")]
        public static bool ComboOrderToPrefabLookupFindPrefabPatch(ComboOrderToPrefabLookup __instance, AssembledDefinitionNode[] ingredients, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || allIngredients == null || !__instance.name.EndsWith("HotChocCosmeticPrefabs")) return true;
            bool[] includeIngredient = new bool[allIngredients.Length];
            for (int i = 0; i < allIngredients.Length; i++)
                for (int j = 0; j < ingredients.Length; j++)
                    if (AssembledDefinitionNode.Matching(ingredients[j], allIngredients[i].Simpilfy()))
                    {
                        includeIngredient[i] = true;
                        break;
                    }
            bool hasMilk = false;
            for (int j = 0; j < ingredients.Length; j++)
                if (AssembledDefinitionNode.Matching(ingredients[j], instance.newRecipes[0].Simpilfy()))
                {
                    hasMilk = true;
                    break;
                }

            var lookupArray = (ComboOrderToPrefabLookup.ContentPrefabLookup[])fieldInfo_m_lookupArray.GetValue(__instance);
            if (!hasMilk) return true;
            int index = (includeIngredient[3] ? 1 : 0) * 2 + (includeIngredient[2] ? 1 : 0) + 3;
            __result = lookupArray[index].m_prefab;
            return false;
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
            allIngredients = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name.EndsWith("HotChocolate")) == null) return;

            allIngredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == "DLC09_" + x || r.name == x || r.name == "DLC03_" + x) as IngredientOrderNode
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == "DLC09_" + x || r.name == x)
            ).ToArray();

            CookedCompositeOrderNode hotMilk = ScriptableObject.CreateInstance<CookedCompositeOrderNode>();
            hotMilk.name = newRecipeData[0].name;
            hotMilk.m_uID = newRecipeData[0].id;
            hotMilk.m_platingPrefab = instance.oldRecipes[0].m_platingPrefab;
            hotMilk.m_platingStep = instance.oldRecipes[0].m_platingStep;
            hotMilk.m_composition = new OrderDefinitionNode[] { allIngredients[1], allIngredients[1] };
            hotMilk.m_cookingStep = (instance.oldRecipes[0] as CookedCompositeOrderNode).m_cookingStep;
            hotMilk.m_progress = CookedCompositeOrderNode.CookingProgress.Cooked;

            instance.newRecipes = new CompositeOrderNode[newRecipeData.Length];
            for (int i = 0; i < newRecipeData.Length; i++)
            {
                CompositeOrderNode newRecipe;
                CompositeOrderNode prefab = instance.oldRecipes[newRecipeData[i].prefab] as CompositeOrderNode;
                if (i == 0)
                    newRecipe = hotMilk;
                else
                {
                    newRecipe = ScriptableObject.CreateInstance<CompositeOrderNode>();
                    newRecipe.name = newRecipeData[i].name;
                    newRecipe.m_uID = newRecipeData[i].id;
                    newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                    newRecipe.m_platingStep = prefab.m_platingStep;
                    var composition = new List<OrderDefinitionNode> { hotMilk };
                    foreach (int x in newRecipeData[i].ingredients)
                        if (x != 1)
                            composition.Add(allIngredients[x]);
                    newRecipe.m_composition = composition.ToArray();
                }

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { allIngredients[1].m_iconSprite, allIngredients[1].m_iconSprite };
                gui1.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0, gui1 };
                foreach (int x in newRecipeData[i].ingredients)
                    if (x != 1)
                    {
                        RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                        gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                        gui2.m_tileDefinition.m_mainPictures = new List<Sprite>() { allIngredients[x].m_iconSprite };
                        children.Add(gui2);
                    }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < newRecipeData.Length; i++)
            {
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.newRecipes[i],
                    m_scoreForMeal = newRecipeData[i].score
                };
                entries.Add(entry);
            }
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8881500, name="HotMilk", ingredients=new int[]{1,1}, prefab=0, score=40 },
            new RecipeData { id=8881501, name="HotMilkCream", ingredients=new int[]{1,1,2}, prefab=1, score=60 },
            new RecipeData { id=8881502, name="HotMilkMallow", ingredients=new int[]{1,1,3}, prefab=2, score=60 },
            new RecipeData { id=8881503, name="HotMilkMallowCream", ingredients=new int[]{1,1,2,3}, prefab=3, score=80 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "HotChocolate",
            "HotChocolateCream",
            "HotChocolateMallow",
            "HotChocolateMallowCream",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            60,
            60,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Chocolate",
            "Milk",
            "WhippedCream",
            "Marshmallow",
        };
    }
}
