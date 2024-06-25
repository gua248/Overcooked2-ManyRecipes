using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class FryPatch : RecipePatchBase
    {
        static FryPatch instance;

        public FryPatch() { instance = this; }

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
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "ChickenNuggetsAndChips_ChickenOnly") == null) return;

            CookedCompositeOrderNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) as CookedCompositeOrderNode
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();

            instance.newRecipes = new CompositeOrderNode[newRecipeData.Length];
            for (int i = 0; i < newRecipeData.Length; i++)
            {
                CompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CompositeOrderNode>();
                CompositeOrderNode prefab = instance.oldRecipes[newRecipeData[i].prefab] as CompositeOrderNode;
                newRecipe.name = newRecipeData[i].name;
                newRecipe.m_uID = newRecipeData[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeData[i].ingredients.Select(x => ingredients[x]).ToArray();

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0 };
                foreach (int x in newRecipeData[i].ingredients)
                {
                    RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                    gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { (ingredients[x].m_composition[0] as IngredientOrderNode).m_iconSprite };
                    gui1.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                    children.Add(gui1);
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
            new RecipeData { id=8881100, name="ChickenNuggetsAndChips_Chicken2", ingredients=new int[]{0,0}, prefab=0, score=60 },
            new RecipeData { id=8881101, name="ChickenNuggetsAndChips_Chips2", ingredients=new int[]{1,1}, prefab=1, score=60 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "ChickenNuggetsAndChips_ChickenOnly",
            "ChickenNuggetsAndChips_ChipsOnly",
            "ChickenNuggetsAndChips_All",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            40,
            60,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "FriedChickenNuggets",
            "FriedChips",
        };
    }
}
