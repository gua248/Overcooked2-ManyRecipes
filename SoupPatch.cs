using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class SoupPatch : RecipePatchBase
    {
        static SoupPatch instance;

        public SoupPatch() { instance = this; }

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
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "OnionPotatoSoupLeek") == null) return;

            IngredientOrderNode[] ingredients = ingredientNames.Select(
                x => (levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)) as IngredientOrderNode
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
            instance.newRecipes = new CookedCompositeOrderNode[newRecipeData.Length];
            for (int i = 0; i < newRecipeData.Length; i++)
            {
                CookedCompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CookedCompositeOrderNode>();
                CookedCompositeOrderNode prefab = instance.oldRecipes[newRecipeData[i].prefab] as CookedCompositeOrderNode;
                newRecipe.name = newRecipeData[i].name;
                newRecipe.m_uID = newRecipeData[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeData[i].ingredients.Select(x => ingredients[x]).ToArray();
                newRecipe.m_cookingStep = prefab.m_cookingStep;
                newRecipe.m_progress = CookedCompositeOrderNode.CookingProgress.Cooked;

                RecipeWidgetUIController.RecipeTileData gui1 = prefab.m_orderGuiDescription[0];
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = newRecipeData[i].ingredients.Select(x => ingredients[x].m_iconSprite).ToList();
                gui2.m_tileDefinition.m_modifierPictures = prefab.m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                newRecipe.m_orderGuiDescription = new RecipeWidgetUIController.RecipeTileData[] { gui1, gui2 };

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            int newRecipeCount = oldEntries.Find(x => x.name == "OnionBroccoliCheeseSoup") == null ? 2 : 4;
            for (int i = 0; i < newRecipeCount; i++)
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
            new RecipeData { id=8880800, name="OOOSoup", ingredients=new int[]{0,0,0}, prefab=1, score=60 },
            new RecipeData { id=8880801, name="PCaLSoup", ingredients=new int[]{1,2,3}, prefab=0, score=60 },
            new RecipeData { id=8880802, name="BBBSoup", ingredients=new int[]{4,4,4}, prefab=2, score=60 },
            new RecipeData { id=8880803, name="PCaChSoup", ingredients=new int[]{2,3,5}, prefab=0, score=60 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "OnionPotatoSoupLeek",
            "OnionCarrotPotatoSoup",
            "OnionBroccoliCheeseSoup",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            60,
            60,
            60,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Onion",
            "DLC07_Potato",
            "Carrot",
            "Leek",
            "Broccoli",
            "DLC07_Cheese",
        };
    }
}
