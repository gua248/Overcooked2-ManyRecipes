using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class PizzaPatch : RecipePatchBase
    {
        static PizzaPatch instance;
        static IngredientOrderNode dough;
        static CompositeOrderNode optionalAssembledPizza;
        static CookedCompositeOrderNode optionalCookedPizza;
        static CookedCompositeOrderNode optionalUncookedPizza;

        public PizzaPatch() { instance = this; }

        static FieldInfo fieldInfo_m_uncookedPrefabLookup = AccessTools.Field(typeof(PizzaCosmeticDecisions), "m_uncookedPrefabLookup");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "StartSynchronising")]
        public static void ClientPreparationContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || dough == null || __instance.GetComponent<PreparationContainer>().m_ingredientOrderNode != dough) return;
            __instance.GetComponent<IngredientContainer>().m_capacity = 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetContentRestrictions")]
        public static void OrderToPrefabLookupGetContentRestrictionsPatch(OrderToPrefabLookup __instance, List<OrderContentRestriction> __result)
        {
            if (!ManyRecipesSettings.enabled || dough == null) return;
            if (__instance.name.EndsWith("RawPizzaCosmeticPrefabs"))
            {
                __result[1].m_amountAllowed = 2;
                __result[2].m_amountAllowed = 2;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetOrderPlatingPrefab", new Type[] { typeof(AssembledDefinitionNode), typeof(PlatingStepData) })]
        public static void GameUtilsGetOrderPlatingPrefabPatch(AssembledDefinitionNode _node, PlatingStepData _platingStep, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || __result != null || optionalAssembledPizza == null || optionalAssembledPizza.m_platingStep != _platingStep) return;
            if (AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalAssembledPizza.Simpilfy()))
                __result = optionalAssembledPizza.m_platingPrefab;
            else if (AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalUncookedPizza.Simpilfy()))
                __result = optionalAssembledPizza.m_platingPrefab;
            else if (AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalCookedPizza.Simpilfy()))
                __result = optionalAssembledPizza.m_platingPrefab;
        }

        static FieldInfo fieldInfo_m_container = AccessTools.Field(typeof(PizzaCosmeticDecisions), "m_container");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PizzaCosmeticDecisions), "CreateRenderChildren")]
        public static void PizzaCosmeticDecisionsCreateRenderChildrenPatch(PizzaCosmeticDecisions __instance)
        {
            Transform container = ((GameObject)fieldInfo_m_container.GetValue(__instance)).transform;
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
            dough = null;
            optionalAssembledPizza = null;
            optionalCookedPizza = null;
            optionalUncookedPizza = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "MargheritaPizza") == null) return;

            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            newRecipeDataTmp.Add(newRecipeData[0]);
            bool hasPepperoni = oldEntries.Find(x => x.name == "PeperoniPizza") != null;
            bool hasChicken = oldEntries.Find(x => x.name == "ChickenPizza") != null;
            bool hasOlive = oldEntries.Find(x => x.name == "Pizza_Olives") != null;
            if (hasPepperoni)
            {
                newRecipeDataTmp.Add(newRecipeData[1]);
                newRecipeDataTmp.Add(newRecipeData[2]);
            }
            if (hasChicken)
            {
                newRecipeDataTmp.Add(newRecipeData[3]);
                newRecipeDataTmp.Add(newRecipeData[4]);
                newRecipeDataTmp.Add(newRecipeData[5]);
            }
            if (hasOlive && hasChicken)
            {
                newRecipeDataTmp.Add(newRecipeData[6]);
                newRecipeDataTmp.Add(newRecipeData[7]);
            }

            IngredientOrderNode[] ingredients = ingredientNames.Select(x => (
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )) as IngredientOrderNode).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            dough = ingredients[5];
            optionalAssembledPizza = ScriptableObject.CreateInstance<CompositeOrderNode>();  
            optionalAssembledPizza.name = "OptionalAssembledPizza";
            optionalAssembledPizza.m_uID = optionalAssembledPizzaID;
            optionalAssembledPizza.m_platingStep = dough.m_platingStep;
            optionalAssembledPizza.m_composition = new OrderDefinitionNode[] { dough };
            optionalUncookedPizza = ScriptableObject.CreateInstance<CookedCompositeOrderNode>();
            optionalUncookedPizza.name = "OptionalUncookedPizza";
            optionalUncookedPizza.m_uID = optionalUncookedPizzaID;
            optionalUncookedPizza.m_platingStep = dough.m_platingStep;
            optionalUncookedPizza.m_composition = new OrderDefinitionNode[] { dough };
            optionalUncookedPizza.m_cookingStep = (instance.oldRecipes[0] as CookedCompositeOrderNode).m_cookingStep;
            optionalUncookedPizza.m_progress = CookedCompositeOrderNode.CookingProgress.Raw;
            optionalCookedPizza = ScriptableObject.CreateInstance<CookedCompositeOrderNode>();
            optionalCookedPizza.name = "OptionalCookedPizza";
            optionalCookedPizza.m_uID = optionalCookedPizzaID;
            optionalCookedPizza.m_platingStep = dough.m_platingStep;
            optionalCookedPizza.m_composition = new OrderDefinitionNode[] { dough };
            optionalCookedPizza.m_cookingStep = (instance.oldRecipes[0] as CookedCompositeOrderNode).m_cookingStep;
            optionalCookedPizza.m_progress = CookedCompositeOrderNode.CookingProgress.Cooked;
            var optional = new List<OrderDefinitionNode>
            {
                ingredients[0],
                ingredients[1], ingredients[1],
                ingredients[2], ingredients[2],
                ingredients[3],
            };
            if (hasOlive)
            {
                optionalAssembledPizza.m_platingPrefab = instance.oldRecipes[3].m_platingPrefab;
                optionalUncookedPizza.m_platingPrefab = instance.oldRecipes[3].m_platingPrefab;
                optionalCookedPizza.m_platingPrefab = instance.oldRecipes[3].m_platingPrefab;
                optional.Add(ingredients[4]);
            }
            else
            {
                optionalAssembledPizza.m_platingPrefab = dough.m_platingPrefab;
                optionalUncookedPizza.m_platingPrefab = dough.m_platingPrefab;
                optionalCookedPizza.m_platingPrefab = dough.m_platingPrefab;
            }
            optionalAssembledPizza.m_optional = optional.ToArray();
            optionalUncookedPizza.m_optional = optional.ToArray();
            optionalCookedPizza.m_optional = optional.ToArray();

            instance.newRecipes = new CookedCompositeOrderNode[newRecipeDataTmp.Count];
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                CookedCompositeOrderNode newRecipe = ScriptableObject.CreateInstance<CookedCompositeOrderNode>();
                CookedCompositeOrderNode prefab = instance.oldRecipes[newRecipeDataTmp[i].prefab] as CookedCompositeOrderNode;
                newRecipe.name = newRecipeDataTmp[i].name;
                newRecipe.m_uID = newRecipeDataTmp[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeDataTmp[i].ingredients.Select(x => ingredients[x]).ToArray();
                newRecipe.m_cookingStep = prefab.m_cookingStep;
                newRecipe.m_progress = CookedCompositeOrderNode.CookingProgress.Cooked;

                RecipeWidgetUIController.RecipeTileData gui1 = prefab.m_orderGuiDescription[0];
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = newRecipeDataTmp[i].ingredients.Select(x => ingredients[x].m_iconSprite).ToList();
                gui2.m_tileDefinition.m_modifierPictures = prefab.m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                newRecipe.m_orderGuiDescription = new RecipeWidgetUIController.RecipeTileData[] { gui1, gui2 };

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
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8880400, name="CCPizza", ingredients=new int[]{5,1,1}, prefab=0, score=80 },
            new RecipeData { id=8880401, name="CSPizza", ingredients=new int[]{5,1,2}, prefab=1, score=80 },
            new RecipeData { id=8880402, name="CCSSPizza", ingredients=new int[]{5,1,1,2,2}, prefab=1, score=120 },
            new RecipeData { id=8880403, name="CKPizza", ingredients=new int[]{5,1,3}, prefab=2, score=80 },
            new RecipeData { id=8880404, name="CSKPizza", ingredients=new int[]{5,1,2,3}, prefab=2, score=100 },
            new RecipeData { id=8880405, name="TCCSKPizza", ingredients=new int[]{5,0,1,1,2,3}, prefab=1, score=140 },
            new RecipeData { id=8880406, name="CCKOPizza", ingredients=new int[]{5,1,1,3,4}, prefab=3, score=120 },
            new RecipeData { id=8880407, name="MegaPizza", ingredients=new int[]{5,0,1,2,3,4}, prefab=3, score=140 },
        };
        static readonly int optionalAssembledPizzaID = 8880408;
        static readonly int optionalCookedPizzaID = 8880409;
        static readonly int optionalUncookedPizzaID = 8880410;
        static readonly string[] oldRecipeNames = new string[]
        {
            "MargheritaPizza",
            "PeperoniPizza",
            "ChickenPizza",
            "Pizza_Olives",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            80,
            100,
            100,
            100,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Tomato",
            "Cheese",
            "Pepperoni",
            "Chicken",
            "Olive",
            "Dough",
        };
    }
}