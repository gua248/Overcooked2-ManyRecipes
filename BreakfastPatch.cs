using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class BreakfastPatch : RecipePatchBase
    {
        static BreakfastPatch instance;

        public BreakfastPatch() { instance = this; }

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
            if (oldEntries.Find(x => x.name == "Breakfast_Bacon_Egg") == null) return;

            IngredientOrderNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) as IngredientOrderNode
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
            for (int i = 0; i < newRecipeData.Length; i++)
            {
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.newRecipes[i],
                    m_scoreForMeal = newRecipeData[i].score
                };
                entries.Add(entry);
            }
            for (int i = 0; i < oldRecipeNames.Length; i++)
                if (oldEntries.Find(x => x.name == oldRecipeNames[i]) == null)
                {
                    RecipeList.Entry entry = new RecipeList.Entry
                    {
                        m_order = instance.oldRecipes[i],
                        m_scoreForMeal = oldEntriesScore[i]
                    };
                    entries.Add(entry);
                }
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8880700, name="Breakfast_E", ingredients=new int[]{0}, prefab=0, score=20 },
            new RecipeData { id=8880701, name="Breakfast_Be", ingredients=new int[]{1}, prefab=2, score=20 },
            new RecipeData { id=8880702, name="Breakfast_EBe", ingredients=new int[]{0,1}, prefab=3, score=40 },
            new RecipeData { id=8880703, name="Breakfast_ES", ingredients=new int[]{0,3}, prefab=1, score=40 },
            new RecipeData { id=8880704, name="Breakfast_EBeB", ingredients=new int[]{0,1,2}, prefab=3, score=60 },
            new RecipeData { id=8880705, name="Breakfast_EEEE", ingredients=new int[]{0,0,0,0}, prefab=0, score=60 },
            new RecipeData { id=8880706, name="Breakfast_EBeBeBe", ingredients=new int[]{0,1,1,1}, prefab=3, score=60 },
            new RecipeData { id=8880707, name="Breakfast_EEBB", ingredients=new int[]{0,0,2,2}, prefab=0, score=80 },
            new RecipeData { id=8880708, name="Breakfast_SSS", ingredients=new int[]{3,3,3}, prefab=1, score=80 },
            new RecipeData { id=8880709, name="Breakfast_BBS", ingredients=new int[]{2,2,3}, prefab=1, score=80 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "Breakfast_Bacon_Egg",
            "Breakfast_Bacon_Egg_Sausage",
            "Breakfast_Sausage_Beans",
            "Breakfast_Sausage_Beans_Egg",
            "Breakfast_Sausage_Beans_Egg_Bacon",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            60,
            40,
            60,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "DLC05_Egg",
            "Beans",
            "Bacon",
            "Sausage",
        };
    }
}
