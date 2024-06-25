using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class HotdogPatch : RecipePatchBase
    {
        static HotdogPatch instance;
        static CompositeOrderNode[] allPrefabs;
        static CompositeOrderNode[] optionalHotdogs;
        static IngredientOrderNode bun;

        public HotdogPatch() { instance = this; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "StartSynchronising")]
        public static void ClientPreparationContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || bun == null || __instance.GetComponent<PreparationContainer>().m_ingredientOrderNode != bun) return;
            __instance.GetComponent<IngredientContainer>().m_capacity = 4;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetContentRestrictions")]
        public static void OrderToPrefabLookupGetContentRestrictionsPatch(OrderToPrefabLookup __instance, List<OrderContentRestriction> __result)
        {
            if (ManyRecipesSettings.enabled && (__instance.name == "DLC08_HotdogPrefabLookup" || __instance.name == "DLC11_HotdogPrefabLookup"))
            {
                __result[2].m_amountAllowed = 2;
                __result[3].m_amountAllowed = 2;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetPrefabForNode")]
        public static void OrderToPrefabLookupGetPrefabForNodePatch(OrderToPrefabLookup __instance, AssembledDefinitionNode _node, ref GameObject __result)
        {
            if (ManyRecipesSettings.enabled && allPrefabs != null && (__instance.name == "HotdogCosmeticPrefabs" || __instance.name == "DLC11_HotdogCosmeticPrefabs") && __result == null)
            {
                var simpleNode = _node.Simpilfy();
                foreach (var hotdog in instance.newRecipes)
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, hotdog.Simpilfy()))
                    {
                        __result = hotdog.m_platingPrefab;
                        return;
                    }
                foreach (int i in new int[] { 0, 2, 4, 6, 7, 9 })
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, optionalHotdogs[i].Simpilfy()))
                    {
                        __result = optionalHotdogs[i].m_platingPrefab;
                        return;
                    }
                foreach (int i in new int[] { 11, 12, 13 })
                    if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, allPrefabs[i].Simpilfy()))
                    {
                        __result = allPrefabs[i].m_platingPrefab;
                        return;
                    }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetOrderPlatingPrefab", new Type[] { typeof(AssembledDefinitionNode), typeof(PlatingStepData) })]
        public static void GameUtilsGetOrderPlatingPrefabPatch(AssembledDefinitionNode _node, PlatingStepData _platingStep, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || allPrefabs == null) return;
            var simpleNode = _node.Simpilfy();
            if (__result == allPrefabs[14].m_platingPrefab)
            {
                if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, allPrefabs[14].Simpilfy()))
                    __result = allPrefabs[17].m_platingPrefab;
                else if (instance.newRecipes.Length >= 3 && AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, instance.newRecipes[2].Simpilfy()))
                    __result = instance.newRecipes[2].m_platingPrefab;
                return;
            }
            if (__result != null || _platingStep != bun.m_platingStep) return;
            foreach (var optionalHotdog in optionalHotdogs)
                if (AssembledDefinitionNode.MatchingAlreadySimple(simpleNode, optionalHotdog.Simpilfy()))
                {
                    __result = optionalHotdog.m_platingPrefab;
                    return;
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
            allPrefabs = null;
            optionalHotdogs = null;
            bun = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "Hotdog_Ketchup" || x.name == "DLC11_Hotdog_Ketchup") == null) return;

            int newRecipeCount = oldEntries.Find(x => x.name == "Hotdog_Onions_Ketchup" || x.name == "DLC11_Hotdog_Onions_Ketchup") == null ? 2 : 3;

            OrderDefinitionNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x || r.name == "DLC11_" + x)
            ).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x || r.name == "DLC11_" + x)
            ).ToArray();
            allPrefabs = allOptionalRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x || r.name == "DLC11_" + x) as CompositeOrderNode
            ).ToArray();
            bun = ingredients[0] as IngredientOrderNode;
            optionalHotdogs = new CompositeOrderNode[10];
            for (int i = 0; i < optionalHotdogs.Length; i++)
            {
                optionalHotdogs[i] = ScriptableObject.CreateInstance<CompositeOrderNode>();
                optionalHotdogs[i].name = $"OptionalHotdog_{i:D2}";
                optionalHotdogs[i].m_uID = newRecipeData[newRecipeData.Length - 1].id + i + 1;
                optionalHotdogs[i].m_platingStep = bun.m_platingStep;
            }
            optionalHotdogs[0].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[4], ingredients[3], ingredients[0] };
            optionalHotdogs[0].m_platingPrefab = allPrefabs[8].m_platingPrefab;
            optionalHotdogs[1].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[4], ingredients[3], ingredients[1] };
            optionalHotdogs[1].m_platingPrefab = allPrefabs[17].m_platingPrefab;
            optionalHotdogs[2].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[3], ingredients[3], ingredients[0] };
            optionalHotdogs[2].m_platingPrefab = allPrefabs[8].m_platingPrefab;
            optionalHotdogs[3].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[3], ingredients[3], ingredients[1] };
            optionalHotdogs[3].m_platingPrefab = allPrefabs[17].m_platingPrefab;
            optionalHotdogs[4].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[4], ingredients[0] };
            optionalHotdogs[4].m_platingPrefab = allPrefabs[7].m_platingPrefab;
            optionalHotdogs[5].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[4], ingredients[1] };
            optionalHotdogs[5].m_platingPrefab = allPrefabs[15].m_platingPrefab;
            optionalHotdogs[6].m_composition = new OrderDefinitionNode[] { ingredients[4], ingredients[4], ingredients[0], ingredients[1] };
            optionalHotdogs[6].m_platingPrefab = allPrefabs[4].m_platingPrefab;
            optionalHotdogs[7].m_composition = new OrderDefinitionNode[] { ingredients[3], ingredients[3], ingredients[0] };
            optionalHotdogs[7].m_platingPrefab = allPrefabs[9].m_platingPrefab;
            optionalHotdogs[8].m_composition = new OrderDefinitionNode[] { ingredients[3], ingredients[3], ingredients[1] };
            optionalHotdogs[8].m_platingPrefab = allPrefabs[16].m_platingPrefab;
            optionalHotdogs[9].m_composition = new OrderDefinitionNode[] { ingredients[3], ingredients[3], ingredients[0], ingredients[1] };
            optionalHotdogs[9].m_platingPrefab = allPrefabs[2].m_platingPrefab;

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
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite> { bun.m_iconSprite };
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = new List<Sprite> { ((ingredients[1] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode).m_iconSprite };
                gui2.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0, gui1, gui2 };
                if (newRecipeData[i].ingredients.Contains(2))
                {
                    RecipeWidgetUIController.RecipeTileData gui3 = new RecipeWidgetUIController.RecipeTileData();
                    gui3.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui3.m_tileDefinition.m_mainPictures = new List<Sprite> { ((ingredients[2] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode).m_iconSprite };
                    gui3.m_tileDefinition.m_modifierPictures = instance.oldRecipes[1].m_orderGuiDescription[3].m_tileDefinition.m_modifierPictures;
                    children.Add(gui3);
                }
                if (newRecipeData[i].ingredients.Contains(3) || newRecipeData[i].ingredients.Contains(4))
                {
                    RecipeWidgetUIController.RecipeTileData gui4 = new RecipeWidgetUIController.RecipeTileData();
                    gui4.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui4.m_tileDefinition.m_mainPictures = new List<Sprite>();
                    foreach (int x in newRecipeData[i].ingredients)
                        if (x == 3 || x == 4)
                            gui4.m_tileDefinition.m_mainPictures.Add((ingredients[x] as IngredientOrderNode).m_iconSprite);
                    children.Add(gui4);
                }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < newRecipeCount; i++)
            {
                int score = levelConfig.name.StartsWith("Summer_1_4") ? 60 : newRecipeData[i].score;
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.newRecipes[i],
                    m_scoreForMeal = score
                };
                entries.Add(entry);
            }
            if (levelConfig.name.StartsWith("Summer_1_1") || levelConfig.name.StartsWith("Summer_1_5"))
            {
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.oldRecipes[0],
                    m_scoreForMeal = oldEntriesScore[0]
                };
                entries.Add(entry);
            }
            if (levelConfig.name.StartsWith("Summer_1_4"))
            {
                foreach (int i in new int[] { 0, 1 })
                {
                    RecipeList.Entry entry = new RecipeList.Entry
                    {
                        m_order = instance.oldRecipes[i],
                        m_scoreForMeal = 60
                    };
                    entries.Add(entry);
                }
            }
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8881600, name="Hotdog_MMK", ingredients=new int[]{0,1,3,3,4}, prefab=6, score=100 },
            new RecipeData { id=8881601, name="Hotdog_MKK", ingredients=new int[]{0,1,3,4,4}, prefab=6, score=100 },
            new RecipeData { id=8881602, name="Hotdog_OMK", ingredients=new int[]{0,1,2,3,4}, prefab=5, score=120 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "Hotdog_Plain",
            "Hotdog_Onions",
            "Hotdog_Mustard",
            "Hotdog_Onions_Mustard",
            "Hotdog_Ketchup",
            "Hotdog_Onions_Ketchup",
            "Hotdog_Ketchup_Mustard",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            80,
            60,
            100,
            60,
            100,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "HotDogBun",
            "BoiledFrankfurter",
            "FriedOnions",
            "Mustard",
            "Ketchup",
        };
        static readonly string[] allOptionalRecipeNames = new string[]
        {
            "Hotdog_Plain",
            "Hotdog_Onions",
            "Hotdog_Mustard",
            "Hotdog_Onions_Mustard",
            "Hotdog_Ketchup",
            "Hotdog_Onions_Ketchup",                        
            "Hotdog_Ketchup_Mustard",                   // 6
            "Optional_Bun_Ketchup",
            "Optional_Bun_Ketchup_Mustard",
            "Optional_Bun_Mustard",
            "Optional_Bun_Onions",
            "Optional_Bun_Onions_Ketchup",
            "Optional_Bun_Onions_Ketchup_Mustard",
            "Optional_Bun_Onions_Mustard",              // 13
            "Optional_Frank_Onions_Ketchup_Mustard",
            "Optional_Frankfurter_Ketchup",
            "Optional_Frankfurter_Mustard",
            "Optional_Frankfurter_Mustard_Ketchup",
            "Optional_Frankfurter_Onions",
            "Optional_Frankfurter_Onions_Ketchup",
            "Optional_Frankfurter_Onions_Mustard",      // 20
            "Optional_Onions_Ketchup",
            "Optional_Onions_Ketchup_Mustard",
            "Optional_Onions_Mustard",
        };
    }
}
