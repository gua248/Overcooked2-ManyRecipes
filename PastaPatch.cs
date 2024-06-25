using HarmonyLib;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OC2ManyRecipes
{
    public class PastaPatch : RecipePatchBase
    {
        static PastaPatch instance;
        static CompositeOrderNode optionalPasta;
        static CookedCompositeOrderNode[] allIngredients;

        public PastaPatch() { instance = this; }

        static FieldInfo fieldInfo_m_lookupArray = AccessTools.Field(typeof(ComboOrderToPrefabLookup), "m_lookupArray");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComboOrderToPrefabLookup), "FindPrefab")]
        public static bool ComboOrderToPrefabLookupFindPrefabPatch(ComboOrderToPrefabLookup __instance, AssembledDefinitionNode[] ingredients, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || allIngredients == null || __instance.name != "PastaCosmeticPrefabs_New") return true;
            if (ingredients.Length == 1 || ingredients.Length >= 4) return true;
            bool[] includeIngredient = new bool[allIngredients.Length];
            for (int i = 0; i < allIngredients.Length; i++)
                for (int j = 0; j < ingredients.Length; j++)
                    if (AssembledDefinitionNode.Matching(ingredients[j], allIngredients[i].Simpilfy()))
                    {
                        includeIngredient[i] = true;
                        break;
                    }
            var lookupArray = (ComboOrderToPrefabLookup.ContentPrefabLookup[])fieldInfo_m_lookupArray.GetValue(__instance);
            if (includeIngredient[0])
            {
                if (ingredients.Length == 2) return true;
                else if (includeIngredient[2] && !includeIngredient[1] && !includeIngredient[3] && !includeIngredient[4] && !includeIngredient[5])
                    __result = lookupArray[6].m_prefab;
                else if (includeIngredient[1] && includeIngredient[2])
                    __result = lookupArray[6].m_prefab;
                else if (includeIngredient[1] && includeIngredient[3])
                    __result = lookupArray[8].m_prefab;
                else if (includeIngredient[1] && includeIngredient[4])
                    __result = lookupArray[7].m_prefab;
                else if (includeIngredient[1] && includeIngredient[5])
                    __result = lookupArray[11].m_prefab;
                else if (includeIngredient[3] && includeIngredient[4])
                    __result = lookupArray[8].m_prefab;
                else if (includeIngredient[2] && includeIngredient[5])
                    __result = lookupArray[11].m_prefab;
                else return true;
            }
            else
            {
                if (ingredients.Length == 3) return true;
                else if (includeIngredient[2] && !includeIngredient[1] && !includeIngredient[3] && !includeIngredient[4] && !includeIngredient[5])
                    __result = lookupArray[1].m_prefab;
                else if (includeIngredient[1] && includeIngredient[2])
                    __result = lookupArray[1].m_prefab;
                else if (includeIngredient[1] && includeIngredient[3])
                    __result = lookupArray[3].m_prefab;
                else if (includeIngredient[1] && includeIngredient[4])
                    __result = lookupArray[2].m_prefab;
                else if (includeIngredient[1] && includeIngredient[5])
                    __result = lookupArray[12].m_prefab;
                else if (includeIngredient[3] && includeIngredient[4])
                    __result = lookupArray[3].m_prefab;
                else if (includeIngredient[2] && includeIngredient[5])
                    __result = lookupArray[12].m_prefab;
                else return true;
            }
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetOrderPlatingPrefab", new Type[] { typeof(AssembledDefinitionNode), typeof(PlatingStepData) })]
        public static void GameUtilsGetOrderPlatingPrefabPatch(AssembledDefinitionNode _node, PlatingStepData _platingStep, ref GameObject __result)
        {
            if (!ManyRecipesSettings.enabled || __result != null || optionalPasta == null) return;
            if (optionalPasta.m_platingStep == _platingStep && AssembledDefinitionNode.MatchingAlreadySimple(_node.Simpilfy(), optionalPasta.Simpilfy()))
                __result = optionalPasta.m_platingPrefab;
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
            optionalPasta = null;
            allIngredients = null;
            if (!ManyRecipesSettings.enabled) return;
            LevelConfigBase levelConfig = __instance.GetLevelConfig();
            if (levelConfig == null || levelConfig.m_recipeMatchingList == null || levelConfig is BossCampaignLevelConfig) return;
            var oldEntries = levelConfig.GetAllRecipes();

            bool hasMeat = oldEntries.Find(x => x.name == "Pasta_MeatOnly_New") != null;
            bool hasTomato = oldEntries.Find(x => x.name == "Pasta_TomatoOnly_New") != null;
            bool hasMushroom = oldEntries.Find(x => x.name == "Pasta_MushroomOnly_New") != null;
            bool hasFish = oldEntries.Find(x => x.name == "Pasta_Marinara_New") != null;
            if (!hasMeat && !hasTomato) return;
            List<RecipeData> newRecipeDataTmp = new List<RecipeData>();
            newRecipeDataTmp.Add(newRecipeData[0]);
            if (hasTomato && !hasMeat)
                newRecipeDataTmp.Add(newRecipeData[1]);
            if (hasMeat && hasMushroom)
                newRecipeDataTmp.Add(newRecipeData[2]);
            if (hasMeat && hasTomato)
                newRecipeDataTmp.Add(newRecipeData[5]);
            if (hasMeat && hasFish)
            {
                newRecipeDataTmp.Add(newRecipeData[3]);
                newRecipeDataTmp.Add(newRecipeData[4]);
            }
            if (hasTomato && hasFish)
            {
                newRecipeDataTmp.Add(newRecipeData[6]);
                newRecipeDataTmp.Add(newRecipeData[7]);
            }

            allIngredients = ingredientNames.Select(x => (
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )) as CookedCompositeOrderNode).ToArray();
            instance.oldRecipes = oldRecipeNames.Select(x =>
                levelConfig.m_recipeMatchingList.m_recipes.FirstOrDefault(r => r.name == x) ?? (
                levelConfig.m_recipeMatchingList.m_includeLists.Length == 0 ?
                null : levelConfig.m_recipeMatchingList.m_includeLists[0].m_recipes.FirstOrDefault(r => r.name == x)
            )).ToArray();
            optionalPasta = ScriptableObject.CreateInstance<CompositeOrderNode>();
            optionalPasta.name = "OptionalPasta";
            optionalPasta.m_uID = optionalPastaID;
            optionalPasta.m_platingStep = allIngredients[0].m_platingStep;
            optionalPasta.m_platingPrefab = allIngredients[0].m_platingPrefab;
            optionalPasta.m_optional = new OrderDefinitionNode[]
            {
                allIngredients[0],
                allIngredients[1],
                allIngredients[2], allIngredients[2],
                allIngredients[3],
                allIngredients[4],
                allIngredients[5],
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
                var children = new List<RecipeWidgetUIController.RecipeTileData> { gui0 };
                foreach (int x in newRecipeDataTmp[i].ingredients)
                {
                    RecipeWidgetUIController.RecipeTileData gui1 = new RecipeWidgetUIController.RecipeTileData();
                    gui1.m_tileDefinition = new RecipeWidgetTile.TileDefinition();
                    gui1.m_tileDefinition.m_mainPictures = new List<Sprite>() { (allIngredients[x].m_composition[0] as IngredientOrderNode).m_iconSprite };
                    if (x == 0)
                        gui1.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[1].m_tileDefinition.m_modifierPictures;
                    else
                        gui1.m_tileDefinition.m_modifierPictures = instance.oldRecipes[0].m_orderGuiDescription[2].m_tileDefinition.m_modifierPictures;
                    children.Add(gui1);
                }
                gui0.m_children = Enumerable.Range(1, children.Count - 1).ToList();
                newRecipe.m_orderGuiDescription = children.ToArray();

                instance.newRecipes[i] = newRecipe;
            }

            List<RecipeList.Entry> entries = new List<RecipeList.Entry>();
            for (int i = 0; i < newRecipeDataTmp.Count; i++)
            {
                int score = (i == 0 && hasTomato && !hasMeat) ? 20 : newRecipeDataTmp[i].score;
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
            new RecipeData { id=8881300, name="Pasta_Empty", ingredients=new int[]{0}, prefab=0, score=40 },
            new RecipeData { id=8881301, name="Pasta_TT", ingredients=new int[]{0,2,2}, prefab=1, score=60 },
            new RecipeData { id=8881302, name="Pasta_MR", ingredients=new int[]{0,1,3}, prefab=2, score=80 },
            new RecipeData { id=8881303, name="Pasta_MF", ingredients=new int[]{0,1,4}, prefab=0, score=80 },
            new RecipeData { id=8881304, name="Pasta_MP", ingredients=new int[]{0,1,5}, prefab=3, score=80 },
            new RecipeData { id=8881305, name="Pasta_MT", ingredients=new int[]{0,1,2}, prefab=1, score=80 },
            new RecipeData { id=8881306, name="Pasta_RF", ingredients=new int[]{0,3,4}, prefab=2, score=80 },
            new RecipeData { id=8881307, name="Pasta_TP", ingredients=new int[]{0,2,5}, prefab=3, score=80 },
        };
        static readonly int optionalPastaID = 8881308;
        static readonly string[] oldRecipeNames = new string[]
        {
            "Pasta_MeatOnly_New",
            "Pasta_TomatoOnly_New",
            "Pasta_MushroomOnly_New",
            "Pasta_Marinara_New",
        };
        static readonly int[] oldEntriesScore = new int[]
        {
            60,
            60,
            60,
            80,
        };
        static readonly string[] ingredientNames = new string[]
        {
            "BoiledPasta",
            "FriedBurritoMeat",
            "PanFriedTomatoes",
            "FriedMushrooms",
            "PanFriedFish",
            "PanFriedPrawns",
        };
    }
}
