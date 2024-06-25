using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OC2ManyRecipes
{
    public class BurritoPatch : RecipePatchBase
    {
        static BurritoPatch instance;
        static IngredientOrderNode tortilla;

        public BurritoPatch() { instance = this; }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "StartSynchronising")]
        public static void ClientPreparationContainerStartSynchronisingPatch(Component __instance)
        {
            if (!ManyRecipesSettings.enabled || tortilla == null || __instance.GetComponent<PreparationContainer>().m_ingredientOrderNode != tortilla) return;
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
            tortilla = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();
            if (oldEntries.Find(x => x.name == "Meat_Burrito") == null) return;

            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            newRecipeDataTmp.Add(newRecipeData[0]);
            bool hasChicken = oldEntries.Find(x => x.name == "Chicken_Burrito") != null;
            bool hasMushroom = oldEntries.Find(x => x.name == "Mushroom_Burrito") != null;
            if (hasMushroom) newRecipeDataTmp.Add(newRecipeData[1]);
            if (hasChicken) newRecipeDataTmp.Add(newRecipeData[2]);
            if (hasMushroom && hasChicken)
            {
                newRecipeDataTmp.Add(newRecipeData[3]);
                newRecipeDataTmp.Add(newRecipeData[4]);
            }

            OrderDefinitionNode[] ingredients = ingredientNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
            tortilla = ingredients[4] as IngredientOrderNode;
            IngredientOrderNode[] raw = new IngredientOrderNode[4];
            for (int i = 0; i < 4; i++)
                raw[i] = (ingredients[i] as CookedCompositeOrderNode).m_composition[0] as IngredientOrderNode;
            instance.oldRecipes = oldRecipeNames.Select(
                x => levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            ).ToArray();
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
                RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { tortilla.m_iconSprite };
                RecipeWidgetUIController.RecipeTileData gui2 = new RecipeWidgetUIController.RecipeTileData();
                gui2.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                gui2.m_tileDefinition.m_mainPictures = new List<Sprite>() { raw[0].m_iconSprite };
                gui2.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0, gui1, gui2 };
                if (newRecipeDataTmp[i].ingredients.Length > 2)
                {
                    RecipeWidgetUIController.RecipeTileData gui3 = new RecipeWidgetUIController.RecipeTileData();
                    gui3.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui3.m_tileDefinition.m_mainPictures = newRecipeDataTmp[i].ingredients.Skip(2).Select(x => raw[x].m_iconSprite).ToList();
                    gui3.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[3].m_tileDefinition.m_modifierPictures;
                    children.Add(gui3);
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
            new RecipeData { id=8880300, name="Empty_Burrito", ingredients=new int[]{4,0}, prefab=1, score=40 },
            new RecipeData { id=8880301, name="MR_Burrito", ingredients=new int[]{4,0,1,2}, prefab=0, score=120 },
            new RecipeData { id=8880302, name="MC_Burrito", ingredients=new int[]{4,0,1,3}, prefab=1, score=120 },
            new RecipeData { id=8880303, name="RC_Burrito", ingredients=new int[]{4,0,2,3}, prefab=2, score=120 },
            new RecipeData { id=8880304, name="Mega_Burrito", ingredients=new int[]{4,0,1,2,3}, prefab=0, score=160 },
        };
        static readonly string[] oldRecipeNames = new string[]
        {
            "Meat_Burrito",
            "Chicken_Burrito",
            "Mushroom_Burrito",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            80,
            80,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "BoiledRice",
            "FriedBurritoMeat",
            "FriedMushrooms",
            "FriedChicken",
            "Tortilla",
        };
    }
}