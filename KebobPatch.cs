using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class KebobPatch : RecipePatchBase
    {
        static KebobPatch instance;
        public static IngredientOrderNode kebobPineapple;

        public KebobPatch() { instance = this; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientIngredientContainer), "StartSynchronising")]
        public static void SetCapacity(Component __instance)
        {
            if (ManyRecipesSettings.enabled && IsSkewer(__instance.gameObject))
                __instance.GetComponent<IngredientContainer>().m_capacity = 5;
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
            if (oldEntries.Find(x => x.name == "ChickenTomatoKebob") == null) return;

            IngredientOrderNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) as IngredientOrderNode
            ).ToArray();
            kebobPineapple = ingredients[4];
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
            {
                if (oldEntries.All(x => x.m_uID != instance.oldRecipes[i].m_uID))
                {
                    RecipeList.Entry entry = new RecipeList.Entry
                    {
                        m_order = instance.oldRecipes[i],
                        m_scoreForMeal = oldEntriesScore[i]
                    };
                    entries.Add(entry);
                }
            }
            instance.entries = entries.ToArray();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KebabCosmeticDecisions), "CreateContents")]
        public static void KebabCosmeticDecisionsCreateContentsPatch(KebabCosmeticDecisions __instance, ref AssembledDefinitionNode _contents)
        {
            if (!ManyRecipesSettings.enabled || instance == null || instance.newRecipes == null) return;
            for (int i = 0; i < newRecipeData.Length; i++)
                if (AssembledDefinitionNode.MatchingAlreadySimple(_contents.Simpilfy(), instance.newRecipes[i].Simpilfy()))
                    _contents = instance.oldRecipes[newRecipeData[i].prefab].Convert();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CookableContainer), "AllowItemPlacement")]
        public static void CookableContainerAllowItemPlacementPatch(CookableContainer __instance, GameObject _object, IBaseCookable _iCookingHandler, ref bool __result)
        {
            if (!ManyRecipesSettings.enabled || _iCookingHandler.IsBurning() || !IsSkewer(__instance.gameObject)) return;
            var ingredient = _object.GetComponent<IngredientPropertiesComponent>();
            if (ingredient != null && SmoothiePatch.smoothiePineapple != null && 
                (ingredient.GetOrderComposition() as IngredientAssembledNode).m_ingriedientOrderNode == SmoothiePatch.smoothiePineapple)
                __result = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IngredientToContainerBehaviour), "TransferToContainer")]
        public static void IngredientToContainerBehaviourTransferToContainerPatch(IngredientToContainerBehaviour __instance, IIngredientContents _container)
        {
            if (!ManyRecipesSettings.enabled || !(_container is ServerIngredientContainer container) || !IsSkewer(container.gameObject)) return;
            var ingredient = __instance.GetComponent<IngredientPropertiesComponent>();
            if (ingredient != null && SmoothiePatch.smoothiePineapple != null &&
                (ingredient.GetOrderComposition() as IngredientAssembledNode).m_ingriedientOrderNode == SmoothiePatch.smoothiePineapple)
                ingredient.SetIngredientOrderNode(kebobPineapple);
        }

        static bool IsSkewer(GameObject gameObject)
        {
            var container = gameObject.GetComponent<CookableContainer>();
            return container != null && container.m_cosmeticsPrefab != null && container.m_cosmeticsPrefab.GetComponent<SkewerCosmeticDecisions>() != null;
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8880100, name="CKebob", ingredients=new int[]{0}, prefab=0, score=40 },
            new RecipeData { id=8880101, name="MKebob", ingredients=new int[]{2}, prefab=1, score=40 },
            new RecipeData { id=8880102, name="RKebob", ingredients=new int[]{3}, prefab=2, score=40 },
            new RecipeData { id=8880103, name="RPKebob", ingredients=new int[]{3,4}, prefab=2, score=60 },
            new RecipeData { id=8880104, name="CMKebob", ingredients=new int[]{0,2}, prefab=1, score=60 },
            new RecipeData { id=8880105, name="TTKebob", ingredients=new int[]{1,1}, prefab=0, score=60 },
            new RecipeData { id=8880106, name="RRRKebob", ingredients=new int[]{3,3,3}, prefab=2, score=80 },
            new RecipeData { id=8880107, name="TMPKebob", ingredients=new int[]{1,2,4}, prefab=3, score=80 },
            new RecipeData { id=8880108, name="CCCKebob", ingredients=new int[]{0,0,0}, prefab=1, score=80 },
            new RecipeData { id=8880109, name="CRPKebob", ingredients=new int[]{0,3,4}, prefab=3, score=80 },
            new RecipeData { id=8880110, name="MMMMKebob", ingredients=new int[]{2,2,2,2}, prefab=1, score=100 },
            new RecipeData { id=8880111, name="CCMMKebob", ingredients=new int[]{0,0,2,2}, prefab=1, score=100 },
            new RecipeData { id=8880112, name="TTPPKebob", ingredients=new int[]{1,1,4,4}, prefab=2, score=100 },
            new RecipeData { id=8880113, name="CMRRKebob", ingredients=new int[]{0,2,3,3}, prefab=3, score=100 },
            new RecipeData { id=8880114, name="TMRPKebob", ingredients=new int[]{1,2,3,4}, prefab=2, score=100 },
            new RecipeData { id=8880115, name="MegaKebob", ingredients=new int[]{0,1,2,3,4}, prefab=3, score=120 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "ChickenTomatoKebob",
            "ChickenMeatTomatoKebob",
            "MushroomPineappleTomatoKebob",
            "MeatMushroomPineappleKebob",
            //"MeatPineappleMushroomChickenKebob",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            60,
            80,
            80,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "KebobChicken_(i)",
            "KebobTomato_(i)",
            "KebobMeat_(i)",
            "KebobMushroom_(i)",
            "KebobPineapple_(i)",
        };
    }
}
