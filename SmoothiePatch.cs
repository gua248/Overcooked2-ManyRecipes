using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class SmoothiePatch : RecipePatchBase
    {
        static SmoothiePatch instance;
        public static IngredientOrderNode smoothiePineapple;

        public SmoothiePatch() { instance = this; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientIngredientContainer), "StartSynchronising")]
        public static void SetCapacity(Component __instance)
        {
            var blender = __instance.GetComponent<BlenderCosmeticDecisions>();
            if (ManyRecipesSettings.enabled && blender != null && blender.m_prefabLookup != null && blender.m_prefabLookup.name == "DLC02_SmoothiePrefabLookup")
                __instance.GetComponent<IngredientContainer>().m_capacity = 4;
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
            if (oldEntries.Find(x => x.name == "BananaSmoothie") == null &&
                oldEntries.Find(x => x.name == "MegaSmoothie") == null) return;

            IngredientOrderNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) as IngredientOrderNode
            ).ToArray();
            smoothiePineapple = ingredients[3];
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
            instance.newRecipes = new MixedCompositeOrderNode[newRecipeData.Length];
            for (int i = 0; i < newRecipeData.Length; i++)
            {
                MixedCompositeOrderNode newRecipe = ScriptableObject.CreateInstance<MixedCompositeOrderNode>();
                OrderDefinitionNode prefab = instance.oldRecipes[newRecipeData[i].prefab];
                newRecipe.name = newRecipeData[i].name;
                newRecipe.m_uID = newRecipeData[i].id;
                newRecipe.m_platingPrefab = prefab.m_platingPrefab;
                newRecipe.m_platingStep = prefab.m_platingStep;
                newRecipe.m_composition = newRecipeData[i].ingredients.Select(x => ingredients[x]).ToArray();
                newRecipe.m_progress = MixedCompositeOrderNode.MixingProgress.Mixed;
                RecipeWidgetUIController.RecipeTileData gui1 = prefab.m_orderGuiDescription[0];
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = newRecipeData[i].ingredients.Select(x => ingredients[x].m_iconSprite).ToList();
                gui2.m_tileDefinition.m_modifierPictures = prefab.m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                newRecipe.m_orderGuiDescription = new RecipeWidgetUIController.RecipeTileData[] { gui1, gui2 };
                instance.newRecipes[i] = newRecipe;
            }

            if (levelConfig.name.StartsWith("Beach_1_1"))
            {
                instance.entries = new RecipeList.Entry[1]
                {
                    new RecipeList.Entry
                    {
                        m_order = instance.newRecipes[8],
                        m_scoreForMeal = newRecipeData[8].score
                    }
                };
            }
            else
            {
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
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MixableContainer), "AllowItemPlacement")]
        public static void MixableContainerAllowItemPlacementPatch(MixableContainer __instance, GameObject _object, bool _overMixed, ref bool __result)
        {
            if (!ManyRecipesSettings.enabled || _overMixed || __instance.GetComponent<BlenderCosmeticDecisions>() == null) return;
            var ingredient = _object.GetComponent<IngredientPropertiesComponent>();
            if (ingredient != null && KebobPatch.kebobPineapple != null &&
                (ingredient.GetOrderComposition() as IngredientAssembledNode).m_ingriedientOrderNode == KebobPatch.kebobPineapple)
                __result = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IngredientToContainerBehaviour), "TransferToContainer")]
        public static void IngredientToContainerBehaviourTransferToContainerPatch(IngredientToContainerBehaviour __instance, IIngredientContents _container)
        {
            if (!ManyRecipesSettings.enabled || !(_container is ServerIngredientContainer container) || container.GetComponent<BlenderCosmeticDecisions>() == null) return;
            var ingredient = __instance.GetComponent<IngredientPropertiesComponent>();
            if (ingredient != null && KebobPatch.kebobPineapple != null &&
                (ingredient.GetOrderComposition() as IngredientAssembledNode).m_ingriedientOrderNode == KebobPatch.kebobPineapple)
                ingredient.SetIngredientOrderNode(smoothiePineapple);
        }

        static readonly RecipeData[] newRecipeData = new RecipeData[]
        {
            new RecipeData { id=8880000, name="PineappleSmoothie", ingredients=new int[]{3,3,3}, prefab=3, score=80 },
            new RecipeData { id=8880001, name="MSBSmoothie", ingredients=new int[]{0,1,2}, prefab=4, score=80 },
            new RecipeData { id=8880002, name="MSPSmoothie", ingredients=new int[]{0,1,3}, prefab=4, score=80 },
            new RecipeData { id=8880003, name="MBPSmoothie", ingredients=new int[]{0,2,3}, prefab=4, score=80 },
            new RecipeData { id=8880004, name="SBPSmoothie", ingredients=new int[]{1,2,3}, prefab=4, score=80 },
            new RecipeData { id=8880005, name="MSSmoothie", ingredients=new int[]{0,0,1,1}, prefab=1, score=100 },
            new RecipeData { id=8880006, name="MBSmoothie", ingredients=new int[]{0,0,2,2}, prefab=2, score=100 },
            new RecipeData { id=8880007, name="MPSmoothie", ingredients=new int[]{0,0,3,3}, prefab=0, score=100 },
            new RecipeData { id=8880008, name="SBSmoothie", ingredients=new int[]{1,1,2,2}, prefab=1, score=100 },
            new RecipeData { id=8880009, name="SPSmoothie", ingredients=new int[]{1,1,3,3}, prefab=1, score=100 }
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "MelonSmoothie",
            "StrawberrySmoothie",
            "BananaSmoothie",
            "BananaPineappleSmoothie",
            "MegaSmoothie",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            80,
            80,
            80,
            100,
            100,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "Melon",
            "SmoothieStrawberry_(i)",
            "Banana",
            "SmoothiePineapple_(i)",
        };
    }
}
