using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class BurgerPatch : RecipePatchBase
    {
        static BurgerPatch instance;
        static IngredientOrderNode bun;
        static CompositeOrderNode optionalBurger;

        public BurgerPatch() { instance = this; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "StartSynchronising")]
        public static void ClientPreparationContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || bun == null || __instance.GetComponent<PreparationContainer>().m_ingredientOrderNode != bun) return;
            __instance.GetComponent<IngredientContainer>().m_capacity = 5;
        }

        static FieldInfo fieldInfo_m_prefabLookup = AccessTools.Field(typeof(BurgerBunCosmeticDecisions), "m_prefabLookup");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetContentRestrictions")]
        public static void OrderToPrefabLookupGetContentRestrictionsPatch(OrderToPrefabLookup __instance, List<OrderContentRestriction> __result)
        {
            if (!ManyRecipesSettings.enabled || bun == null) return;
            if (__instance.name == "DLC02_BurgerCosmeticPrefabs")
            {
                __result[2].m_amountAllowed = 2;
                return;
            }
            if ((OrderToPrefabLookup)fieldInfo_m_prefabLookup.GetValue(bun.m_platingPrefab.GetComponent<BurgerBunCosmeticDecisions>()) != __instance) return;
            __result[2].m_amountAllowed = 2;
            __result[3].m_amountAllowed = 2;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetOrderPlatingPrefab", new Type[] { typeof(AssembledDefinitionNode), typeof(PlatingStepData) })]
        public static void GameUtilsGetOrderPlatingPrefabPatch(AssembledDefinitionNode _node, PlatingStepData _platingStep, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || __result != null || optionalBurger == null) return;
            if (optionalBurger.m_platingStep == _platingStep && AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalBurger.Simpilfy()))
                __result = optionalBurger.m_platingPrefab;
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
            bun = null;
            optionalBurger = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "BeefBurger") == null && 
                oldEntries.Find(x => x.name == "HawaiianBurger") == null) return;

            int newRecipeCount = 2;
            if (oldEntries.Find(x => x.name == "BeefBurgerCheese") != null) newRecipeCount = 4;
            if (oldEntries.Find(x => x.name == "BeefBurgerWithGreensNCheese") != null) newRecipeCount = 5;
            if (oldEntries.Find(x => x.name == "BeefBurgerMax") != null) newRecipeCount = 10;
            if (oldEntries.Find(x => x.name == "HawaiianBurger") != null) newRecipeCount = 15;

            OrderDefinitionNode[] ingredients = ingredientNames.Select(x => 
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ? 
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(x => 
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ? 
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            bun = ingredients[5] as IngredientOrderNode;
            optionalBurger = ScriptableObject.CreateInstance<CompositeOrderNode>();
            optionalBurger.name = "OptionalBurger";
            optionalBurger.m_uID = optionalBurgerID;
            optionalBurger.m_platingStep = bun.m_platingStep;
            optionalBurger.m_composition = new OrderDefinitionNode[] { bun };
            if (!levelConfig.name.StartsWith("Resort"))
            {
                optionalBurger.m_platingPrefab = bun.m_platingPrefab;
                optionalBurger.m_optional = new OrderDefinitionNode[]
                {
                    ingredients[0], ingredients[0],
                    ingredients[1], ingredients[1],
                    ingredients[2],
                    ingredients[3],
                };
            }
            else
            {
                optionalBurger.m_platingPrefab = instance.oldRecipes[4].m_platingPrefab;
                optionalBurger.m_optional = new OrderDefinitionNode[]
                {
                    ingredients[0], ingredients[0], ingredients[0],
                    ingredients[1], ingredients[1],
                    ingredients[2],
                    ingredients[3],
                    ingredients[4],
                };
            }
            IngredientOrderNode meat = (ingredients[0] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode;

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
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { bun.m_iconSprite };
                children.Add(gui1);
                int meatCount = newRecipeData[i].ingredients.Count(x => x == 0);
                if (meatCount > 0)
                {
                    RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                    gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui2.m_tileDefinition.m_mainPictures = Enumerable.Repeat(meat.m_iconSprite, meatCount).ToList();
                    gui2.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                    children.Add(gui2);
                }
                if (meatCount < newRecipeData[i].ingredients.Length - 1)
                {
                    RecipeWidgetUIController.RecipeTileData gui3 = new RecipeWidgetUIController.RecipeTileData();
                    gui3.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui3.m_tileDefinition.m_mainPictures = new List<Sprite>();
                    foreach (int x in newRecipeData[i].ingredients)
                        if (x != 5 && x != 0)
                            gui3.m_tileDefinition.m_mainPictures.Add((ingredients[x] as IngredientOrderNode).m_iconSprite);
                    children.Add(gui3);
                }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();
                
                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            if (levelConfig.name.StartsWith("Resort_1_3"))
            {
                foreach (int i in new int[] { 7, 11 })
                {
                    RecipeList.Entry entry = new RecipeList.Entry
                    {
                        m_order = instance.newRecipes[i],
                        m_scoreForMeal = 100
                    };
                    entries.Add(entry);
                }
            }
            else
            {
                for (int i = 0; i < newRecipeCount; i++)
                {
                    RecipeList.Entry entry = new RecipeList.Entry
                    {
                        m_order = instance.newRecipes[i],
                        m_scoreForMeal = newRecipeData[i].score
                    };
                    entries.Add(entry);
                }
            }
            if (levelConfig.name.StartsWith("MovingPlatform_Level3"))
            {
                RecipeList.Entry entry = new RecipeList.Entry
                {
                    m_order = instance.oldRecipes[1],
                    m_scoreForMeal = oldEntriesScore[1]
                };
                entries.Add(entry);
            }
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8880200, name="EmptyBurger", ingredients=new int[]{5}, prefab=0, score=0 },
            new RecipeData { id=8880201, name="MMBurger", ingredients=new int[]{5,0,0}, prefab=0, score=80 },
            new RecipeData { id=8880202, name="CBurger", ingredients=new int[]{5,1,1}, prefab=1, score=40 },
            new RecipeData { id=8880203, name="MMCCBurger", ingredients=new int[]{5,0,0,1,1}, prefab=1, score=120 },
            new RecipeData { id=8880204, name="MLBurger", ingredients=new int[]{5,0,2}, prefab=3, score=60 },
            new RecipeData { id=8880205, name="MTBurger", ingredients=new int[]{5,0,3}, prefab=2, score=60 },
            new RecipeData { id=8880206, name="CLTBurger", ingredients=new int[]{5,1,2,3}, prefab=2, score=60 },
            new RecipeData { id=8880207, name="MCTBurger", ingredients=new int[]{5,0,1,3}, prefab=1, score=80 },
            new RecipeData { id=8880208, name="MCLTBurger", ingredients=new int[]{5,0,1,2,3}, prefab=3, score=100 },
            new RecipeData { id=8880209, name="MMCLTBurger", ingredients=new int[]{5,0,0,1,2,3}, prefab=2, score=140 },
            new RecipeData { id=8880210, name="MPBurger", ingredients=new int[]{5,0,4}, prefab=4, score=60 },
            new RecipeData { id=8880211, name="MCPBurger", ingredients=new int[]{5,0,1,4}, prefab=4, score=80 },
            new RecipeData { id=8880212, name="MMCCPBurger", ingredients=new int[]{5,0,0,1,1,4}, prefab=4, score=140 },
            new RecipeData { id=8880213, name="MCLTPBurger", ingredients=new int[]{5,0,1,2,3,4}, prefab=4, score=120 },
            new RecipeData { id=8880214, name="MegaBurger", ingredients=new int[]{5,0,0,0,1,2,3,4}, prefab=4, score=200 },
        };
        static readonly int optionalBurgerID = 8880215;
        static readonly string[] oldRecipeNames = new string[]
        {
            "BeefBurger",
            "BeefBurgerCheese",
            "BeefBurgerMax",
            "BeefBurgerWithGreensNCheese",
            "HawaiianBurger",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            40,
            60,
            80,
            80,
            100,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "FriedMeat",
            "Cheese",
            "Lettuce",
            "Tomato",
            "BurgerPineapple_(i)",
            "Bun",
        };
    }
}
