using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class RoastPatch : RecipePatchBase
    {
        static RoastPatch instance;

        public RoastPatch() { instance = this; }

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
            if (oldEntries.Find(x => x.name == "BeefPotatoCarrotRoast") == null) return;

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
            int newRecipeCount = oldEntries.Find(x => x.name == "BeefPotatoCarrotBroccoliRoast") == null ? 7 : 9;
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
            new RecipeData { id=8880600, name="BRoast", ingredients=new int[]{0}, prefab=0, score=20 },
            new RecipeData { id=8880601, name="CRoast", ingredients=new int[]{1}, prefab=2, score=20 },
            new RecipeData { id=8880602, name="BBBBRoast", ingredients=new int[]{0,0,0,0}, prefab=0, score=60 },
            new RecipeData { id=8880603, name="CCCCRoast", ingredients=new int[]{1,1,1,1}, prefab=2, score=60 },
            new RecipeData { id=8880604, name="PoPoCaCaRoast", ingredients=new int[]{2,2,3,3}, prefab=0, score=100 },
            new RecipeData { id=8880605, name="BCaCaCaRoast", ingredients=new int[]{0,3,3,3}, prefab=0, score=80 },
            new RecipeData { id=8880606, name="CPoPoPoRoast", ingredients=new int[]{1,2,2,2}, prefab=2, score=80 },
            new RecipeData { id=8880607, name="BCaBroRoast", ingredients=new int[]{0,3,4}, prefab=1, score=60 },
            new RecipeData { id=8880608, name="CPoBroRoast", ingredients=new int[]{1,2,4}, prefab=3, score=60 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "BeefPotatoCarrotRoast",
            "BeefPotatoCarrotBroccoliRoast",
            "ChickenPotatoCarrotRoast",
            "ChickenPotatoCarrotBroccoliRoast",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            60,
            60,
            80,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Beef_Roast",
            "Chicken_Roast",
            "DLC07_Potato",
            "Carrot",
            "Broccoli",
        };
    }
}