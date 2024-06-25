using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class SteamedPatch : RecipePatchBase
    {
        static SteamedPatch instance;

        public SteamedPatch() { instance = this; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientIngredientContainer), "StartSynchronising")]
        public static void SetCapacity(Component __instance)
        {
            var container = __instance.GetComponent<CookableContainer>();
            if (ManyRecipesSettings.enabled && container != null && container.m_approvedContentsList != null && container.m_approvedContentsList.name == "SteamerPrefabLookup")
                __instance.GetComponent<IngredientContainer>().m_capacity = 2;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CookableContainer), "GetOrderComposition")]
        public static bool CookableContainerGetOrderCompositionPatch(CookableContainer __instance, ref AssembledDefinitionNode __result, IIngredientContents _itemContainer, IBaseCookable _cookingHandler)
        {
            if (!ManyRecipesSettings.enabled || __instance.m_approvedContentsList == null || __instance.m_approvedContentsList.name != "SteamerPrefabLookup")
                return true;
            var composition = _itemContainer.GetContents();
            CookedCompositeAssembledNode[] nodes = new CookedCompositeAssembledNode[composition.Length];
            for (int i = 0; i < composition.Length; i++)
            {
                nodes[i] = new CookedCompositeAssembledNode();
                nodes[i].m_composition = new AssembledDefinitionNode[] { composition[i] };
                nodes[i].m_cookingStep = _cookingHandler.AccessCookingType;
                nodes[i].m_recordedProgress = new float?(_cookingHandler.GetCookingProgress() / _cookingHandler.AccessCookingTime);
                nodes[i].m_progress = _cookingHandler.GetCookedOrderState();
            }
            if (composition.Length == 1)
                __result = nodes[0];
            else
            {
                var node = new CompositeAssembledNode();
                node.m_composition = nodes;
                __result = node;
            }
            return false;
        }

        static FieldInfo fieldInfo_m_container = AccessTools.Field(typeof(SteamedSpecialCosmeticDecisions), "m_container");
        static FieldInfo fieldInfo_m_comboPrefabLookup = AccessTools.Field(typeof(SteamedSpecialCosmeticDecisions), "m_comboPrefabLookup");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComboCosmeticDecisions), "CreateContents")]
        public static bool ComboCosmeticDecisionsCreateContentsPatch(ComboCosmeticDecisions __instance, AssembledDefinitionNode _contents)
        {
            if (ManyRecipesSettings.enabled && 
                __instance is SteamedSpecialCosmeticDecisions &&
                !(_contents is CookedCompositeAssembledNode) &&
                _contents is CompositeAssembledNode contents && 
                contents.m_composition.Length > 1)
            {
                var lookup = (ComboOrderToPrefabLookup)fieldInfo_m_comboPrefabLookup.GetValue(__instance);
                var container = (GameObject)fieldInfo_m_container.GetValue(__instance);
                for (int i = 0; i < contents.m_composition.Length; i++)
                {
                    GameObject prefabForNode = lookup.GetPrefabForNode(contents.m_composition[i]);
                    if (prefabForNode != null)
                    {
                        GameObject gameObject = prefabForNode.InstantiateOnParent(container.transform, __instance.m_maintainScale);
                        gameObject.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                        gameObject.transform.localPosition = new Vector3(0.2f * (i == 0 ? 1 : -1), 0f, 0f);
                    }
                }
                return false;
            }
            return true;
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
            bool[] hasEntry = oldRecipeNames.Select(name => oldEntries.Find(x => x.name == name) != null).ToArray();
            if (!hasEntry.Any(x => x)) return;

            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            for (int i = 0 ; i < newRecipeData.Length; i++)
                if (hasEntry[newRecipeData[i].ingredients[0]] && hasEntry[newRecipeData[i].ingredients[1]])
                    newRecipeDataTmp.Add(newRecipeData[i]);

            CookedCompositeOrderNode[] ingredients = ingredientNames.Select(x => (
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )) as CookedCompositeOrderNode).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();

            instance.newRecipes = new CompositeOrderNode[newRecipeDataTmp.Count];
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                CompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CompositeOrderNode>();
                CompositeOrderNode prefab = instance.oldRecipes[newRecipeDataTmp[i].prefab] as CompositeOrderNode;
                newRecipe.name = newRecipeDataTmp[i].name;
                newRecipe.m_uID = newRecipeDataTmp[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeDataTmp[i].ingredients.Select(x => ingredients[x]).ToArray();

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0 };
                foreach (int x in newRecipeDataTmp[i].ingredients)
                {
                    RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                    gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui1.m_tileDefinition.m_mainPictures = ingredients[x].m_orderGuiDescription[1].m_tileDefinition.m_mainPictures;
                    gui1.m_tileDefinition.m_modifierPictures = ingredients[x].m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                    children.Add(gui1);
                }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                if (newRecipeDataTmp[i].ingredients[0] == newRecipeDataTmp[i].ingredients[1]) continue;
                int score = newRecipeDataTmp[i].score;
                var prawnEntry = (levelConfig as KitchenLevelConfigBase).GetRoundData().m_recipes.m_recipes.FirstOrDefault(x => x.m_order == instance.oldRecipes[2]);
                if (newRecipeDataTmp[i].ingredients.Contains(2) && prawnEntry != null && prawnEntry.m_scoreForMeal == 60) 
                    score -= 20;
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.newRecipes[i],
                    m_scoreForMeal = score
                };
                entries.Add(entry);
            }
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8881800, name="SteamedSpecial_MF", ingredients=new int[]{0,1}, prefab=0, score=120 },
            new RecipeData { id=8881801, name="SteamedSpecial_PF", ingredients=new int[]{2,1}, prefab=2, score=120 },
            new RecipeData { id=8881802, name="SteamedSpecial_CF", ingredients=new int[]{3,1}, prefab=3, score=120 },
            new RecipeData { id=8881803, name="SteamedSpecial_MP", ingredients=new int[]{0,2}, prefab=2, score=160 },
            new RecipeData { id=8881804, name="SteamedSpecial_MC", ingredients=new int[]{0,3}, prefab=0, score=160 },
            new RecipeData { id=8881805, name="SteamedSpecial_PC", ingredients=new int[]{2,3}, prefab=3, score=160 },
            new RecipeData { id=8881806, name="SteamedSpecial_MM", ingredients=new int[]{0,0}, prefab=0, score=160 },
            new RecipeData { id=8881807, name="SteamedSpecial_FF", ingredients=new int[]{1,1}, prefab=1, score=80 },
            new RecipeData { id=8881808, name="SteamedSpecial_PP", ingredients=new int[]{2,2}, prefab=2, score=160 },
            new RecipeData { id=8881809, name="SteamedSpecial_CC", ingredients=new int[]{3,3}, prefab=3, score=160 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "SteamedSpecial_Meat",
            "SteamedSpecial_Fish",
            "SteamedSpecial_Prawns",
            "SteamedSpecial_Carrot",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            80,
            40,
            80,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "SteamedSpecial_Meat",
            "SteamedSpecial_Fish",
            "SteamedSpecial_Prawns",
            "SteamedSpecial_Carrot",
        };
    }
}
