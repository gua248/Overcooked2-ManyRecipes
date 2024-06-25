using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class MDPatch : RecipePatchBase
    {
        static MDPatch instance;
        static OrderDefinitionNode[] allIngredients;
        static CompositeOrderNode[] burgers;
        static CompositeOrderNode optionalBurger;

        public MDPatch() { instance = this; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientIngredientContainer), "StartSynchronising")]
        public static void ClientTrayIngredientContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || allIngredients == null || !(__instance is ClientTrayIngredientContainer)) return;
            __instance.GetComponent<TrayIngredientContainer>().m_capacity = 5;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "StartSynchronising")]
        public static void ClientPreparationContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || allIngredients == null || __instance.GetComponent<PreparationContainer>().m_ingredientOrderNode != allIngredients[0]) return;
            __instance.GetComponent<IngredientContainer>().m_capacity = 2;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OrderToPrefabLookup), "GetContentRestrictions")]
        public static void OrderToPrefabLookupGetContentRestrictionsPatch(OrderToPrefabLookup __instance, List<OrderContentRestriction> __result)
        {
            if (!ManyRecipesSettings.enabled || burgers == null) return;
            if (__instance.name == "DLC08_BurgerCosmeticPrefabs")
            {
                __result[0].m_amountAllowed = 2;
                __result[1].m_amountAllowed = 2;
                __result[0].m_restrictedContent = new OrderDefinitionNode[0];
                __result[1].m_restrictedContent = new OrderDefinitionNode[0];
            }
            if (__instance.name == "TrayBurgerSlot")
            {
                foreach (int i in new int[] { 1, 2 })
                    __result.Add(new OrderContentRestriction
                    {
                        m_content = allIngredients[i],
                        m_amountAllowed = 1,
                        m_restrictedContent = new OrderDefinitionNode[0],
                    });
                foreach (var burger in burgers)
                    __result.Add(new OrderContentRestriction
                    {
                        m_content = burger,
                        m_amountAllowed = 1,
                        m_restrictedContent = new OrderDefinitionNode[0],
                    });
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tray), "GetIngredientContents")]
        public static void TrayGetIngredientContentsPatch(Tray __instance, List<int> _availableSlots)
        {
            List<int> tmp = new List<int>();
            for (int i = 0; i < __instance.m_slots.Length; i++)
                if (_availableSlots.Contains(i))
                    tmp.Add(i);
            _availableSlots.Clear();
            _availableSlots.AddRange(tmp);
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
            allIngredients = null;
            burgers = null;
            optionalBurger = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null) return;
            var oldEntries = levelConfig.GetAllRecipes();

            bool hasMeat = oldEntries.Find(x => x.name == "MD_Burger_Fries") != null;
            bool hasChicken = oldEntries.Find(x => x.name == "MD_C_Burger_OnionRings") != null;
            bool hasDrink = oldEntries.Find(x => x.name == "MD_Burger_Drink01") != null;
            if (!hasMeat && !hasChicken) return;
            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            if (hasMeat)
            {
                newRecipeDataTmp.Add(newRecipeData[0]);
                newRecipeDataTmp.Add(newRecipeData[1]);
                if (hasDrink)
                    newRecipeDataTmp.Add(newRecipeData[4]);
            }
            if (hasChicken)
            {
                newRecipeDataTmp.Add(newRecipeData[2]);
                newRecipeDataTmp.Add(newRecipeData[3]);
                if (hasDrink)
                    newRecipeDataTmp.Add(newRecipeData[5]);
            }
            if (hasMeat && hasChicken && hasDrink)
            {
                newRecipeDataTmp.Add(newRecipeData[6]);
                newRecipeDataTmp.Add(newRecipeData[7]);
                newRecipeDataTmp.Add(newRecipeData[8]);
            }

            allIngredients = ingredientNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
            burgers = new CompositeOrderNode[3];
            for (int i = 0; i < burgers.Length; i++)
            {
                burgers[i] = ScriptableObject.CreateInstance<CompositeOrderNode>();
                burgers[i].name = $"NewBurger_{i:D2}";
                burgers[i].m_uID = newRecipeData[newRecipeData.Length - 1].id + i + 1;
                burgers[i].m_platingStep = allIngredients[0].m_platingStep;
                burgers[i].m_platingPrefab = allIngredients[0].m_platingPrefab;
            }
            burgers[0].m_composition = new OrderDefinitionNode[] { allIngredients[0], allIngredients[1], allIngredients[1] };
            burgers[1].m_composition = new OrderDefinitionNode[] { allIngredients[0], allIngredients[1], allIngredients[2] };
            burgers[2].m_composition = new OrderDefinitionNode[] { allIngredients[0], allIngredients[2], allIngredients[2] };
            optionalBurger = ScriptableObject.CreateInstance<CompositeOrderNode>();
            optionalBurger.name = "OptionalBurger";
            optionalBurger.m_uID = newRecipeData[newRecipeData.Length - 1].id + burgers.Length + 1;
            optionalBurger.m_platingStep = allIngredients[0].m_platingStep;
            optionalBurger.m_platingPrefab = allIngredients[0].m_platingPrefab;
            optionalBurger.m_optional = new OrderDefinitionNode[]
            {
                allIngredients[0],
                allIngredients[1], allIngredients[1],
                allIngredients[2], allIngredients[2],
            };

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
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { (allIngredients[0] as IngredientOrderNode).m_iconSprite };
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = new List<Sprite>()
                {
                    ((allIngredients[newRecipeDataTmp[i].ingredients[1]] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode).m_iconSprite,
                    ((allIngredients[newRecipeDataTmp[i].ingredients[2]] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode).m_iconSprite,
                };
                gui2.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0, gui1, gui2 };
                foreach (int x in newRecipeDataTmp[i].ingredients)
                {
                    if (x == 3 || x == 4 || x == 5)
                    {
                        RecipeWidgetUIController.RecipeTileData gui3 = new RecipeWidgetUIController.RecipeTileData();
                        gui3.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                        gui3.m_tileDefinition.m_mainPictures = new List<Sprite>() { ((allIngredients[x] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode).m_iconSprite };
                        gui3.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[3].m_tileDefinition.m_modifierPictures;
                        children.Add(gui3);
                    }
                    else if (x == 6 || x == 7 || x == 8)
                    {
                        RecipeWidgetUIController.RecipeTileData gui3 = new RecipeWidgetUIController.RecipeTileData();
                        gui3.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                        gui3.m_tileDefinition.m_mainPictures = new List<Sprite>() { (allIngredients[x] as IngredientOrderNode).m_iconSprite };
                        children.Add(gui3);
                    }
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
            instance.entries = entries.ToArray();
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8881900, name="MD_Burger2_CheeseSticks",                ingredients=new int[]{0,1,1,5}, prefab=0, score=120 },
            new RecipeData { id=8881901, name="MD_Burger2_Fries_OnionRings",            ingredients=new int[]{0,1,1,3,4}, prefab=3, score=160 },
            new RecipeData { id=8881902, name="MD_C_Burger2_Fries",                     ingredients=new int[]{0,2,2,3}, prefab=6, score=120 },
            new RecipeData { id=8881903, name="MD_C_Burger2_OnionRings_CheeseSticks",   ingredients=new int[]{0,2,2,4,5}, prefab=5, score=160 },
            new RecipeData { id=8881904, name="MD_Burger2_OnionRings_Drink02",          ingredients=new int[]{0,1,1,4,7}, prefab=10, score=140 },
            new RecipeData { id=8881905, name="MD_C_Burger2_CheeseSticks_Drink01",      ingredients=new int[]{0,2,2,5,6}, prefab=14, score=140 },
            new RecipeData { id=8881906, name="MD_2Burger_OnionRings",                  ingredients=new int[]{0,1,2,4}, prefab=4, score=120 },
            new RecipeData { id=8881907, name="MD_2Burger_Fries_Drink03",               ingredients=new int[]{0,1,2,3,8}, prefab=13, score=140 },
            new RecipeData { id=8881908, name="MD_2Burger_Fries_CheeseSticks",          ingredients=new int[]{0,1,2,3,5}, prefab=1, score=160 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "MD_Burger_Fries",
            "MD_Burger_Fries_CheeseSticks",
            "MD_Burger_OnionRings",
            "MD_Burger_OnionRings_CheeseSticks",
            "MD_C_Burger_OnionRings",
            "MD_C_Burger_Fries_OnionRings",
            "MD_C_Burger_CheeseSticks",
            "MD_C_Burger_Fries_CheeseSticks",
            "MD_Burger_Drink01",
            "MD_Burger_OnionRings_Drink01",
            "MD_Burger_Fries_Drink02",
            "MD_Burger_CheeseSticks_Drink03",
            "MD_C_Burger_CheeseSticks_Drink02",
            "MD_C_Burger_Fries_Drink03",
            "MD_C_Burger_OnionRings_Drink01",
            "MD_C_Burger_Drink03",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
        };
        static readonly string[] ingredientNames = new string[]
        {
            "DLC08_Bun",
            "FriedMeat",
            "FriedChickenBurger",
            "DLC08_FriedChips",
            "FriedOnion_Rings",
            "FriedCheese_Sticks",
            "Drink01",
            "Drink02",
            "Drink03",
        };
    }
}
