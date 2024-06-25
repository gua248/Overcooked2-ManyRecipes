using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class HotPotPatch : RecipePatchBase
    {
        static HotPotPatch instance;

        public HotPotPatch() { instance = this; }

        static FieldInfo fieldInfo_m_lookupArray = AccessTools.Field(typeof(OrderToPrefabLookup), "m_lookupArray");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetPrefabForNode")]
        public static void OrderToPrefabLookupGetPrefabForNodePatch(OrderToPrefabLookup __instance, AssembledDefinitionNode _node, ref GameObject __result)
        {
            if (ManyRecipesSettings.enabled && 
                __instance.name.EndsWith("LargePotCookableObjectsLookup") &&
                __result == null && 
                _node.Simpilfy() != AssembledDefinitionNode.NullNode)
            {
                __result = ((OrderToPrefabLookup.ContentPrefabLookup[])fieldInfo_m_lookupArray.GetValue(__instance))[4].m_prefab;
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
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name.EndsWith("HotPot_Meat")) == null) return;

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
            new RecipeData { id=8880500, name="DLC10_HotPot_NNNN", ingredients=new int[]{0,0,0,0}, prefab=0, score=60 },
            new RecipeData { id=8880501, name="DLC10_HotPot_MMPP", ingredients=new int[]{2,2,3,3}, prefab=2, score=100 },
            new RecipeData { id=8880502, name="DLC10_HotPot_NMP", ingredients=new int[]{0,2,3}, prefab=2, score=60 },
            new RecipeData { id=8880503, name="DLC10_HotPot_NG", ingredients=new int[]{0,1}, prefab=1, score=40 },
            new RecipeData { id=8880504, name="DLC10_HotPot_GGMP", ingredients=new int[]{1,1,2,3}, prefab=2, score=100 },
            new RecipeData { id=8880505, name="DLC10_HotPot_MMMM", ingredients=new int[]{2,2,2,2}, prefab=3, score=100 },
            new RecipeData { id=8880506, name="DLC10_HotPot_PPPP", ingredients=new int[]{3,3,3,3}, prefab=4, score=100 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "HotPot_Meat",
            "HotPot_Prawn",
            "HotPot_Mixed",
            "HotPot_DoubleMeat",
            "HotPot_DoublePrawn",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            60,
            60,
            80,
            80,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Noodles",
            "BokChoy",
            "DLC04_Meat",
            "DLC04_Prawn",
        };
    }
}