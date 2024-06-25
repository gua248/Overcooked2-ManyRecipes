using GameModes;
using GameModes.Horde;
using HarmonyLib;
using OC2ManyRecipes.Extension;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OC2ManyRecipes
{
    public class RecipeData
    {
        public int[] ingredients;
        public int prefab;
        public string name;
        public int id;
        public int score;
    }

    public class RecipePatchBase
    {
        public OrderDefinitionNode[] newRecipes = null;
        public OrderDefinitionNode[] oldRecipes = null;
        public RecipeList.Entry[] entries = null;
    }

    public static class Patch
    {
        static int GetNewRecipeCount()
        {
            return ManyRecipesPlugin.recipePatches.Sum(x => x.newRecipes == null ? 0 : x.newRecipes.Length);
        }

        static int GetNewEntryCount()
        {
            return ManyRecipesPlugin.recipePatches.Sum(x => x.entries == null ? 0 : x.entries.Length);
        }

        static RecipeList.Entry GetNewRecipeEntry(RecipeList.Entry[] oldRecipes, int index)
        {
            if (index < oldRecipes.Length) return oldRecipes[index];
            index -= oldRecipes.Length;
            foreach (var recipePatch in ManyRecipesPlugin.recipePatches)
                if (recipePatch.entries != null)
                {
                    if (index < recipePatch.entries.Length) return recipePatch.entries[index];
                    index -= recipePatch.entries.Length;
                }
            return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetAllOrderNodes")]
        public static void GameUtilsGetAllOrderNodesPatch(ref OrderDefinitionNode[] __result)
        {
            if (!ManyRecipesSettings.enabled) return;
            OrderDefinitionNode[] allRecipes = new OrderDefinitionNode[__result.Length + GetNewRecipeCount()];
            __result.CopyTo(allRecipes, 0);
            int i = __result.Length;
            foreach (var patch in ManyRecipesPlugin.recipePatches)
                if (patch.newRecipes != null)
                {
                    patch.newRecipes.CopyTo(allRecipes, i);
                    i += patch.newRecipes.Length;
                }
            __result = allRecipes;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUtils), "GetAllOrderNodesSimplified")]
        public static void GameUtilsGetAllOrderNodesSimplifiedPatch(ref AssembledDefinitionNode[] __result)
        {
            if (!ManyRecipesSettings.enabled) return;
            AssembledDefinitionNode[] allRecipes = new AssembledDefinitionNode[__result.Length + GetNewRecipeCount()];
            __result.CopyTo(allRecipes, 0);
            int i = __result.Length;
            foreach (var patch in ManyRecipesPlugin.recipePatches)
                if (patch.newRecipes != null)
                {
                    patch.newRecipes.Select(x => x.Convert().Simpilfy()).ToArray().CopyTo(allRecipes, i);
                    i += patch.newRecipes.Length;
                }
            __result = allRecipes;
        }

        class RoundDataForPatch : DynamicRoundData
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(RoundData), "InitialiseRound")]
            public static bool RoundDataInitialiseRoundPatch(RoundData __instance, ref RoundInstanceDataBase __result)
            {
                if (!ManyRecipesSettings.enabled) return true;
                __result = new RoundInstanceData
                {
                    CumulativeFrequencies = new int[__instance.m_recipes.m_recipes.Length + GetNewEntryCount()]
                };
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(RoundData), "GetNextRecipe")]
            public static bool RoundDataGetNextRecipePatch(RoundData __instance, RoundInstanceDataBase _data, ref RecipeList.Entry[] __result)
            {
                if (!ManyRecipesSettings.enabled) return true;
                RoundInstanceData data = _data as RoundInstanceData;
                data.RecipeCount++;
                int num = data.CumulativeFrequencies.Collapse((int f, int total) => total + f);
                float num2 = (num + 2) / (float)(__instance.m_recipes.m_recipes.Length + GetNewEntryCount());
                float[] weight = data.CumulativeFrequencies.Select(x => Mathf.Max(num2 - x, 0f)).ToArray();
                KeyValuePair<int, float> weightedRandomElement = weight.GetWeightedRandomElement((int i, float w) => weight[i]);
                data.CumulativeFrequencies[weightedRandomElement.Key]++;
                __result = new RecipeList.Entry[]
                {
                    GetNewRecipeEntry(__instance.m_recipes.m_recipes, weightedRandomElement.Key)
                };
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DynamicRoundData), "InitialiseRound")]
            public static bool DynamicRoundDataInitialiseRoundPatch(DynamicRoundData __instance, ref RoundInstanceDataBase __result)
            {
                if (!ManyRecipesSettings.enabled) return true;
                var levelConfig = GameUtils.GetLevelConfig();
                int n = GetNewEntryCount();
                if (levelConfig != null && levelConfig.name.StartsWith("5_6_Dynamic_Lvl_03"))
                    n -= 4;
                if (levelConfig != null && levelConfig.name.StartsWith("1_6_Dynamic_Lvl_01"))
                    n -= 5;
                __result = new DynamicRoundInstanceData
                {
                    CumulativeFrequencies = new int[__instance.Phases[0].Recipes.m_recipes.Length + n]
                };
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DynamicRoundData), "MoveToNextPhase")]
            public static bool DynamicRoundDataMoveToNextPhasePatch(DynamicRoundData __instance, ref RoundInstanceDataBase _data)
            {
                if (!ManyRecipesSettings.enabled) return true;
                if (__instance.GetRemainingPhases(_data) > 0)
                {
                    DynamicRoundInstanceData dynamicRoundInstanceData = _data as DynamicRoundInstanceData;
                    dynamicRoundInstanceData.CurrentPhase++;
                    Phase phase = __instance.Phases[dynamicRoundInstanceData.CurrentPhase];
                    dynamicRoundInstanceData.RecipeCount = 0;
                    var levelConfig = GameUtils.GetLevelConfig();
                    int n = GetNewEntryCount();
                    if (levelConfig != null && levelConfig.name.StartsWith("5_6_Dynamic_Lvl_03") && dynamicRoundInstanceData.CurrentPhase != 2)
                        n -= 4;
                    if (levelConfig != null && levelConfig.name.StartsWith("1_6_Dynamic_Lvl_01"))
                        n -= dynamicRoundInstanceData.CurrentPhase == 1 ? 5 : 3;
                    dynamicRoundInstanceData.CumulativeFrequencies = new int[phase.Recipes.m_recipes.Length + n];
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DynamicRoundData), "GetNextRecipe")]
            public static bool DynamicRoundDataGetNextRecipePatch(DynamicRoundData __instance, RoundInstanceDataBase _data, ref RecipeList.Entry[] __result)
            {
                if (!ManyRecipesSettings.enabled) return true;
                DynamicRoundInstanceData data = _data as DynamicRoundInstanceData;
                Phase phase = __instance.Phases[data.CurrentPhase];
                data.RecipeCount++;
                int num = data.CumulativeFrequencies.Collapse((int f, int total) => total + f);
                var levelConfig = GameUtils.GetLevelConfig();
                int n = GetNewEntryCount();
                if (levelConfig != null && levelConfig.name.StartsWith("5_6_Dynamic_Lvl_03") && data.CurrentPhase != 2)
                    n -= 4;
                if (levelConfig != null && levelConfig.name.StartsWith("1_6_Dynamic_Lvl_01"))
                    n -= data.CurrentPhase <= 1 ? 5 : 3;
                float num2 = (num + 2) / (float)(phase.Recipes.m_recipes.Length + n);

                float[] weight = data.CumulativeFrequencies.Select(x => Mathf.Max(num2 - x, 0f)).ToArray();
                KeyValuePair<int, float> weightedRandomElement = weight.GetWeightedRandomElement((int i, float w) => weight[i]);
                data.CumulativeFrequencies[weightedRandomElement.Key]++;
                int k = weightedRandomElement.Key;
                if (levelConfig != null && levelConfig.name.StartsWith("1_6_Dynamic_Lvl_01") &&
                    data.CurrentPhase <= 1 && k >= phase.Recipes.m_recipes.Length)
                    k += 5;
                __result = new RecipeList.Entry[]
                {
                    GetNewRecipeEntry(phase.Recipes.m_recipes, k)
                };
                return false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ServerOrderControllerBase), "Update")]
        public static void ServerOrderControllerBaseUpdatePatch(ServerOrderControllerBase __instance)
        {
            if (!ManyRecipesSettings.enabledAddMenu) return;

            int cnt = __instance.ActiveRecipes.Count;
            int min = ServerGameSetup.Mode == GameMode.Versus ? 3 : 4;
            int max = ServerGameSetup.Mode == GameMode.Versus ? 4 : 6;
            if (__instance.get_m_autoProgress() && cnt < max && (__instance.get_m_timerUntilOrder() < 0f || cnt < min))
            {
                __instance.AddNewOrder();
                __instance.set_m_timerUntilOrder(__instance.GetNextTimeBetweenOrders());
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RecipeFlowGUI), "GetMaxOrderNumber")]
        public static bool RecipeFlowGUIGetMaxOrderNumberPatch(ref int __result)
        {
            if (ManyRecipesSettings.enabled)
            {
                __result = 6;
                return false;
            }
            return true;
        }
        
        static readonly FieldInfo fieldInfo_m_levelConfig = AccessTools.Field(typeof(ServerHordeFlowController), "m_levelConfig");
        static readonly FieldInfo fieldInfo_m_waves = AccessTools.Field(typeof(HordeWavesData), "m_waves");

        class RecipeMoneyDataPatch : RecipeMoneyData
        {
            public void Add(RecipeList.Entry[] entries)
            {
                foreach (var entry in entries)
                {
                    m_keys.Add(entry.m_order);
                    m_values.Add(entry.m_scoreForMeal);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerHordeFlowController), "RunWaves")]
        public static void ServerHordeFlowControllerRunWavesPatch(ServerHordeFlowController __instance, ref HordeWavesData waves)
        {
            if (!ManyRecipesSettings.enabled) return;
            HordeLevelConfig newConfig = ScriptableObject.CreateInstance<HordeLevelConfig>();
            HordeLevelConfig oldConfig = (HordeLevelConfig)fieldInfo_m_levelConfig.GetValue(__instance);
            HordeWavesData oldWaves = oldConfig.m_waves;
            List<HordeWaveData> newWaves = new List<HordeWaveData>();
            for (int i = 0; i < oldWaves.Count; i++)
            {
                HordeWaveData newWave = oldWaves[i];
                newWave.m_recipes = new RecipeList();
                newWave.m_recipes.m_recipes = new RecipeList.Entry[oldWaves[i].m_recipes.m_recipes.Length + GetNewEntryCount()];
                oldWaves[i].m_recipes.m_recipes.CopyTo(newWave.m_recipes.m_recipes, 0);
                int cnt = oldWaves[i].m_recipes.m_recipes.Length;
                foreach (var patch in ManyRecipesPlugin.recipePatches)
                    if (patch.entries != null)
                    {
                        patch.entries.CopyTo(newWave.m_recipes.m_recipes, cnt);
                        cnt += patch.entries.Length;
                    }
                newWaves.Add(newWave);
            }
            object w = newConfig.m_waves;
            fieldInfo_m_waves.SetValue(w, newWaves);
            newConfig.m_waves = (HordeWavesData)w;

            RecipeMoneyDataPatch newRecipeMoney = new RecipeMoneyDataPatch();
            newRecipeMoney.Add(oldWaves[oldWaves.Count - 1].m_recipes.m_recipes);
            foreach (var patch in ManyRecipesPlugin.recipePatches)
                if (patch.entries != null)
                    newRecipeMoney.Add(patch.entries);
            newConfig.m_recipeMoney = newRecipeMoney;

            fieldInfo_m_levelConfig.SetValue(__instance, newConfig);
            waves = newConfig.m_waves;
            //IEnumerator run = (IEnumerator)methodInfo_RunWaves.Invoke(__instance, new object[] { newConfig.m_waves, __instance.GetComponent<HordeFlowController>().m_waveNumberUIDelay });
            //fieldInfo_m_runWaves.SetValue(__instance, run);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(T17TabPanel), "OnTabSelected")]
        public static void T17TabPanelOnTabSelectedPatch()
        {
            ManyRecipesSettings.AddUI();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FrontendCoopTabOptions), "OnOnlinePublicClicked")]
        [HarmonyPatch(typeof(FrontendVersusTabOptions), "OnOnlinePublicClicked")]
        public static bool OnOnlinePublicClickedPatch()
        {
            ManyRecipesSettings.enabled = false;
            ManyRecipesSettings.enabledAddMenu = false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ToggleOption), "OnToggleButtonPressed")]
        public static bool ToggleOptionOnToggleButtonPressedPatch(ToggleOption __instance, ref bool bValue)
        {
            if (__instance == ManyRecipesSettings.manyRecipesOption)
            {
                ManyRecipesSettings.enabled = bValue;
                if (bValue == false && ManyRecipesSettings.enabledAddMenu == true)
                {
                    ManyRecipesSettings.enabledAddMenu = false;
                    ManyRecipesSettings.manyRecipesOptionAddMenu.GetComponent<T17Toggle>().isOn = false;
                }
                return false;
            }
            else if (__instance == ManyRecipesSettings.manyRecipesOptionAddMenu)
            {
                if (ManyRecipesSettings.enabled)
                    ManyRecipesSettings.enabledAddMenu = bValue;
                else if (ManyRecipesSettings.manyRecipesOptionAddMenu.GetComponent<T17Toggle>().isOn)
                    ManyRecipesSettings.manyRecipesOptionAddMenu.GetComponent<T17Toggle>().isOn = false;
                return false;
            }
            return true;
        }

        public static void PatchAll(Harmony patcher)
        {
            patcher.PatchAll(typeof(Patch));
            patcher.PatchAll(typeof(RoundDataForPatch));
        }
    }
}
