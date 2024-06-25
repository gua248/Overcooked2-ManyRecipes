using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class SmorePatch : RecipePatchBase
    {
        static SmorePatch instance;
        static IngredientOrderNode crackers;
        static CompositeOrderNode optionalSmore;

        public SmorePatch() { instance = this; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "StartSynchronising")]
        public static void ClientPreparationContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || crackers == null || __instance.GetComponent<PreparationContainer>().m_ingredientOrderNode != crackers) return;
            __instance.GetComponent<IngredientContainer>().m_capacity = 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetContentRestrictions")]
        public static void OrderToPrefabLookupGetContentRestrictionsPatch(OrderToPrefabLookup __instance, List<OrderContentRestriction> __result)
        {
            if (ManyRecipesSettings.enabled && __instance.name == "DLC05_SmoresLookUp")
            {
                __result[0].m_amountAllowed = 2;
                __result[3].m_amountAllowed = 2;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetOrderPlatingPrefab", new Type[] { typeof(AssembledDefinitionNode), typeof(PlatingStepData) })]
        public static void GameUtilsGetOrderPlatingPrefabPatch(AssembledDefinitionNode _node, PlatingStepData _platingStep, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || __result != null || optionalSmore == null) return;
            if (optionalSmore.m_platingStep == _platingStep && AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalSmore.Simpilfy()))
                __result = optionalSmore.m_platingPrefab;
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
            crackers = null;
            optionalSmore = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "Smores_Plain") == null) return;

            int newRecipeCount = 3;
            if (oldEntries.Find(x => x.name == "Smores_Banana") != null) newRecipeCount = 4;
            if (oldEntries.Find(x => x.name == "Smores_Strawberry") != null) newRecipeCount = 7;

            OrderDefinitionNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
            crackers = ingredients[4] as IngredientOrderNode;
            optionalSmore = ScriptableObject.CreateInstance<CompositeOrderNode>();
            optionalSmore.name = "optionalSmore";
            optionalSmore.m_uID = optionalSmoreID;
            optionalSmore.m_platingStep = crackers.m_platingStep;
            optionalSmore.m_composition = new OrderDefinitionNode[] { crackers };
            optionalSmore.m_platingPrefab = crackers.m_platingPrefab;
            optionalSmore.m_optional = new OrderDefinitionNode[]
            {
                ingredients[0], ingredients[0],
                ingredients[1], ingredients[1],
                ingredients[2],
                ingredients[3],
            };
            IngredientOrderNode marshmallow = (ingredients[0] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode;

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

                RecipeWidgetUIController.RecipeTileData gui0 = new RecipeWidgetUIController.RecipeTileData();
                gui0.m_tileDefinition = prefab.m_orderGuiDescription[0].m_tileDefinition;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0 };
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { crackers.m_iconSprite };
                children.Add(gui1);
                int marshmallowCount = newRecipeData[i].ingredients.Count(x => x == 0);
                if (marshmallowCount > 0)
                {
                    RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                    gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui2.m_tileDefinition.m_mainPictures = Enumerable.Repeat(marshmallow.m_iconSprite, marshmallowCount).ToList();
                    gui2.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                    children.Add(gui2);
                }
                if (marshmallowCount < newRecipeData[i].ingredients.Length - 1)
                {
                    RecipeWidgetUIController.RecipeTileData gui3 = new RecipeWidgetUIController.RecipeTileData();
                    gui3.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui3.m_tileDefinition.m_mainPictures = new List<Sprite>();
                    foreach (int x in newRecipeData[i].ingredients)
                        if (x != 4 && x != 0)
                            gui3.m_tileDefinition.m_mainPictures.Add((ingredients[x] as IngredientOrderNode).m_iconSprite);
                    children.Add(gui3);
                }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();

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
            new RecipeData { id=8880900, name="Smores_Empty", ingredients=new int[]{4}, prefab=0, score=0 },
            new RecipeData { id=8880901, name="Smores_MM", ingredients=new int[]{4,0,0}, prefab=0, score=80 },
            new RecipeData { id=8880902, name="Smores_MMCC", ingredients=new int[]{4,0,0,1,1}, prefab=1, score=120 },
            new RecipeData { id=8880903, name="Smores_MCB", ingredients=new int[]{4,0,1,2}, prefab=2, score=80 },
            new RecipeData { id=8880904, name="Smores_MCS", ingredients=new int[]{4,0,1,3}, prefab=3, score=80 },
            new RecipeData { id=8880905, name="Smores_MCBS", ingredients=new int[]{4,0,1,2,3}, prefab=4, score=100 },
            new RecipeData { id=8880906, name="Smores_Mega", ingredients=new int[]{4,0,0,1,2,3}, prefab=4, score=140 },
        };
        static readonly int optionalSmoreID = 8880907;
        static readonly string[] oldRecipeNames = new string[]
        {
            "Smores_Plain",
            "Smores_Chocolate",
            "Smores_Banana",
            "Smores_Strawberry",
            "Smores_Strawberry_Banana",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            60,
            60,
            60,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "RoastedMarshmallow",
            "DLC05_Chocolate",
            "DLC05_Banana",
            "DLC05_Strawberry",
            "Crackers",
        };
    }
}
