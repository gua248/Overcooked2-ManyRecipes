using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class SaladPatch : RecipePatchBase
    {
        static SaladPatch instance;

        public SaladPatch() { instance = this; }

        static FieldInfo fieldInfo_m_container = AccessTools.Field(typeof(SaladCosmeticDecisions), "m_container");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OverlapModelsMealDecisions), "CreateRenderChildren")]
        public static void OverlapModelsMealDecisionsCreateRenderChildrenPatch(OverlapModelsMealDecisions __instance)
        {
            if (__instance is SaladCosmeticDecisions salad)
            {
                Transform container = ((GameObject)fieldInfo_m_container.GetValue(salad)).transform;
                if (container == null) return;
                Dictionary<string, int> count = new Dictionary<string, int>();
                for (int i = 0; i < container.childCount; i++)
                {
                    var child = container.GetChild(i);
                    if (count.ContainsKey(child.name)) count[child.name]++;
                    else count[child.name] = 1;
                    child.localRotation = Quaternion.Euler(0, 36 * (count[child.name] - 1), 0);
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
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "Salad_Plain") == null) return;

            int newRecipeCount = oldEntries.Find(x => x.name == "Salad_Cucumber") == null ? 2 : 3;

            IngredientOrderNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) as IngredientOrderNode
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();

            instance.newRecipes = new CompositeOrderNode[newRecipeCount];
            for (int i = 0; i < newRecipeCount; i++)
            {
                CompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CompositeOrderNode>();
                CompositeOrderNode prefab = instance.oldRecipes[newRecipeData[i].prefab] as CompositeOrderNode;
                newRecipe.name = newRecipeData[i].name;
                newRecipe.m_uID = newRecipeData[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeData[i].ingredients.Select(x => ingredients[x]).ToArray();

                RecipeWidgetUIController.RecipeTileData gui1 = prefab.m_orderGuiDescription[0];
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = newRecipeData[i].ingredients.Select(x => ingredients[x].m_iconSprite).ToList();
                newRecipe.m_orderGuiDescription = new RecipeWidgetUIController.RecipeTileData[] { gui1, gui2 };

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
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
            new RecipeData { id=8881400, name="Salad_LL", ingredients=new int[]{0,0}, prefab=0, score=40 },
            new RecipeData { id=8881401, name="Salad_LL", ingredients=new int[]{1,1}, prefab=1, score=40 },
            new RecipeData { id=8881402, name="Salad_LC", ingredients=new int[]{0,2}, prefab=2, score=40 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "Salad_Plain",
            "Salad_Tomato",
            "Salad_Cucumber",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            20,
            40,
            60,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Lettuce",
            "Tomato",
            "Cucumber",
        };
    }
}
