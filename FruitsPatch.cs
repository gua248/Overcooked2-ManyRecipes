using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class FruitsPatch : RecipePatchBase
    {
        static FruitsPatch instance;

        public FruitsPatch() { instance = this; }

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
            if (oldEntries.Find(x => x.name.EndsWith("FruitPlatter_GrapesPeach")) == null) return;

            IngredientOrderNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x || r.name == "DLC04_" + x || r.name == "DLC13_" + x) as IngredientOrderNode
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name.EndsWith(x))
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
                    gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { ingredients[x].m_iconSprite };
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
            for (int i = 0; i < oldRecipeNames.Length; i++)
                if (oldEntries.Find(x => x.name.EndsWith(oldRecipeNames[i])) == null)
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
            new RecipeData { id=8881200, name="FruitPlatter_Orange2", ingredients=new int[]{0,0}, prefab=0, score=40 },
            new RecipeData { id=8881201, name="FruitPlatter_Peach2", ingredients=new int[]{1,1}, prefab=0, score=40 },
            new RecipeData { id=8881202, name="FruitPlatter_Grapes2", ingredients=new int[]{2,2}, prefab=1, score=40 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "FruitPlatter_OrangePeach",
            "FruitPlatter_GrapesPeach",
            "FruitPlatter_OrangeGrapes",
            "FruitPlatter_OrangePeachGrapes",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            40,
            40,
            60,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Orange",
            "Peach",
            "Grapes",
        };
    }
}
