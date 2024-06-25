using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class SushiPatch : RecipePatchBase
    {
        static SushiPatch instance;
        static CompositeOrderNode optionalSushi;
        static OrderDefinitionNode[] allIngredients;

        public SushiPatch() { instance = this; }

        static FieldInfo fieldInfo_m_lookupArray = AccessTools.Field(typeof(ComboOrderToPrefabLookup), "m_lookupArray");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComboOrderToPrefabLookup), "FindPrefab")]
        public static bool ComboOrderToPrefabLookupFindPrefabPatch(ComboOrderToPrefabLookup __instance, AssembledDefinitionNode[] ingredients, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || allIngredients == null || __instance.name != "SushiCosmeticPrefabs") return true;
            bool[] includeIngredient = new bool[allIngredients.Length];
            for (int i = 0; i < allIngredients.Length; i++)
                for (int j = 0; j < ingredients.Length; j++)
                    if (AssembledDefinitionNode.Matching(ingredients[j], allIngredients[i].Simpilfy()))
                    {
                        includeIngredient[i] = true;
                        break;
                    }
            var lookupArray = (ComboOrderToPrefabLookup.ContentPrefabLookup[])fieldInfo_m_lookupArray.GetValue(__instance);
            if (includeIngredient[4] && !includeIngredient[0] && !includeIngredient[1])
            {
                if (!includeIngredient[3])
                    __result = lookupArray[8].m_prefab;
                else
                    __result = lookupArray[14].m_prefab;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetOrderPlatingPrefab", new Type[] { typeof(AssembledDefinitionNode), typeof(PlatingStepData) })]
        public static void GameUtilsGetOrderPlatingPrefabPatch(AssembledDefinitionNode _node, PlatingStepData _platingStep, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || __result != null || optionalSushi == null) return;
            if (optionalSushi.m_platingStep == _platingStep && AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalSushi.Simpilfy()))
                __result = optionalSushi.m_platingPrefab;
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
            optionalSushi = null;
            allIngredients = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            bool[] hasEntry = oldRecipeNames.Select(name => oldEntries.Find(x => x.name == name) != null).ToArray();
            if (!hasEntry.Any(x => x)) return;

            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            if (hasEntry[0] && hasEntry[1])
                newRecipeDataTmp.Add(newRecipeData[0]);
            if (hasEntry[2] || hasEntry[3])
                newRecipeDataTmp.Add(newRecipeData[1]);
            if (hasEntry[2])
                newRecipeDataTmp.Add(newRecipeData[2]);
            if (hasEntry[1] && (hasEntry[2] || hasEntry[3]))
                newRecipeDataTmp.Add(newRecipeData[3]);
            if (hasEntry[3])
                newRecipeDataTmp.Add(newRecipeData[4]);
            if (hasEntry[1] && hasEntry[2] && hasEntry[3])
                newRecipeDataTmp.Add(newRecipeData[5]);

            allIngredients = ingredientNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            optionalSushi = ScriptableObject.CreateInstance<CompositeOrderNode>();
            optionalSushi.name = "OptionalSushi";
            optionalSushi.m_uID = optionalSushiID;
            optionalSushi.m_platingStep = instance.oldRecipes[0].m_platingStep;
            optionalSushi.m_platingPrefab = instance.oldRecipes[0].m_platingPrefab;
            optionalSushi.m_optional = new OrderDefinitionNode[]
            {
                allIngredients[0], 
                allIngredients[1], 
                allIngredients[2],
                allIngredients[3],
                allIngredients[4],
            };
            IngredientOrderNode rice = (allIngredients[0] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode;

            instance.newRecipes = new CompositeOrderNode[newRecipeDataTmp.Count];
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                CompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CompositeOrderNode>();
                CompositeOrderNode prefab = instance.oldRecipes[newRecipeDataTmp[i].prefab] as CompositeOrderNode;
                newRecipe.name = newRecipeDataTmp[i].name;
                newRecipe.m_uID = newRecipeDataTmp[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeDataTmp[i].ingredients.Select(x => allIngredients[x]).ToArray();

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0 };
                foreach (int x in newRecipeDataTmp[i].ingredients)
                {
                    RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                    gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    if (x == 0)
                    {
                        gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { rice.m_iconSprite };
                        gui1.m_tileDefinition.m_modifierPictures = instance.oldRecipes[2].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                    }
                    else
                    {
                        gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { (allIngredients[x] as IngredientOrderNode).m_iconSprite };
                    }
                    children.Add(gui1);
                }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.newRecipes[i],
                    m_scoreForMeal = newRecipeDataTmp[i].score
                };
                entries.Add(entry);
            }
            List<int> addOldRecipeIndex = new List<int>();
            if (hasEntry[2] && !hasEntry[0]) addOldRecipeIndex.Add(0);
            if (hasEntry[2] && hasEntry[3] && !hasEntry[4]) addOldRecipeIndex.Add(4);
            foreach (int i in addOldRecipeIndex)
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
            new RecipeData { id=8881000, name="Sushi_FP", ingredients=new int[]{2,4}, prefab=1, score=40 },
            new RecipeData { id=8881001, name="Sushi_RN", ingredients=new int[]{1,0}, prefab=3, score=40 },
            new RecipeData { id=8881002, name="Sushi_RF", ingredients=new int[]{0,2}, prefab=2, score=60 },
            new RecipeData { id=8881003, name="Sushi_RP", ingredients=new int[]{0,4}, prefab=2, score=60 },
            new RecipeData { id=8881004, name="Sushi_C", ingredients=new int[]{3}, prefab=3, score=20 },
            new RecipeData { id=8881005, name="Sushi_NFCP", ingredients=new int[]{1,2,3,4}, prefab=4, score=60 },
        };
        static readonly int optionalSushiID = 8881006;
        static readonly string[] oldRecipeNames = new string[]
        {
            "Sushi_PlainFish",
            "Sushi_PlainPrawn",
            "Sushi_Fish",
            "Sushi_Cucumber",
            "Sushi_All",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            20,
            20,
            60,
            60,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "BoiledSushiRice",
            "Seaweed",
            "SushiFish",
            "Cucumber",
            "SushiPrawn",
        };
    }
}
