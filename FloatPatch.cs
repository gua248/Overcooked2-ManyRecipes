using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class FloatPatch : RecipePatchBase
    {
        static FloatPatch instance;

        public FloatPatch() { instance = this; }

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
            if (oldEntries.Find(x => x.name == "OrangeSodaFloat_Vanilla") == null) return;

            IngredientOrderNode[] ingredients = ingredientNames.Select(x => (
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )) as IngredientOrderNode).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();

            MixedCompositeOrderNode ice2 = ScriptableObject.CreateInstance<MixedCompositeOrderNode>();
            ice2.name = "IceCream_Ice";
            ice2.m_uID = newRecipeData[2].id;
            ice2.m_platingPrefab = instance.oldRecipes[4].m_platingPrefab;
            ice2.m_platingStep = instance.oldRecipes[4].m_platingStep;
            ice2.m_composition = new OrderDefinitionNode[] { ingredients[0], ingredients[1], ingredients[1] };
            ice2.m_progress = MixedCompositeOrderNode.MixingProgress.Mixed;

            instance.newRecipes = new CompositeOrderNode[newRecipeData.Length];
            instance.newRecipes[2] = ice2;
            for (int i = 0; i < 2; i++)
            {
                CompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CompositeOrderNode>();
                CompositeOrderNode prefab = instance.oldRecipes[newRecipeData[i].prefab] as CompositeOrderNode;
                newRecipe.name = newRecipeData[i].name;
                newRecipe.m_uID = newRecipeData[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = new OrderDefinitionNode[] { ice2, ingredients[newRecipeData[i].ingredients[0]] };

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                gui0.m_children = new List<int> { 1, 2 };
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { ingredients[1].m_iconSprite, ingredients[1].m_iconSprite, ingredients[0].m_iconSprite };
                gui1.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = new List<Sprite>() { ingredients[newRecipeData[i].ingredients[0]].m_iconSprite };
                newRecipe.m_orderGuiDescription = new RecipeWidgetUIController.RecipeTileData[] { gui0, gui1, gui2 };

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < 2; i++)
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
            new RecipeData { id=8881700, name="OrangeSodaFloat_Ice", ingredients=new int[]{4}, prefab=0, score=60 },
            new RecipeData { id=8881701, name="RootBeerFloat_Ice", ingredients=new int[]{5}, prefab=2, score=60 },
            new RecipeData { id=8881702, name="IceCream_Ice", ingredients=new int[]{}, prefab=4, score=0 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "OrangeSodaFloat_Vanilla",
            "OrangeSodaFloat_Chocolate",
            "RootBeerFloat_Vanilla",
            "RootBeerFloat_Chocolate",
            "IceCream_Vanilla",
            "IceCream_Chocolate",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
        };
        static readonly string[] ingredientNames = new string[]
        {
            "DLC11_Milk",
            "IceCube",
            "Chocolate",
            "Vanilla",
            "OrangeSoda",
            "RootBeer",
        };
    }
}
