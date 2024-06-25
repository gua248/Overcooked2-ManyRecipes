using HarmonyLib;
using System.Reflection;
using UnityEngine.UI;

namespace OC2ManyRecipes.Extension
{
    public static class ServerOrderControllerBaseExtension
    {
        static readonly FieldInfo fieldInfo_m_autoProgress = AccessTools.Field(typeof(ServerOrderControllerBase), "m_autoProgress");
        static readonly FieldInfo fieldInfo_m_timerUntilOrder = AccessTools.Field(typeof(ServerOrderControllerBase), "m_timerUntilOrder");
        static readonly MethodInfo methodInfo_GetNextTimeBetweenOrders = AccessTools.Method(typeof(ServerFixedTimeOrderController), "GetNextTimeBetweenOrders");

        public static bool get_m_autoProgress(this ServerOrderControllerBase instance)
        {
            return (bool)fieldInfo_m_autoProgress.GetValue(instance);
        }

        public static float get_m_timerUntilOrder(this ServerOrderControllerBase instance)
        {
            return (float)fieldInfo_m_timerUntilOrder.GetValue(instance);
        }

        public static void set_m_timerUntilOrder(this ServerOrderControllerBase instance, float value)
        {
            fieldInfo_m_timerUntilOrder.SetValue(instance, value);
        }

        public static float GetNextTimeBetweenOrders(this ServerOrderControllerBase instance)
        {
            return (float)methodInfo_GetNextTimeBetweenOrders.Invoke(instance as ServerFixedTimeOrderController, null);
        }
    }

    public static class FrontendRootMenuExtension
    {
        static readonly FieldInfo fieldInfo_m_CurrentGamepadUser = AccessTools.Field(typeof(FrontendRootMenu), "m_CurrentGamepadUser");
        static readonly MethodInfo methodInfo_OnMenuHide = AccessTools.Method(typeof(FrontendRootMenu), "OnMenuHide");
        static readonly MethodInfo methodInfo_OnMenuShow = AccessTools.Method(typeof(FrontendRootMenu), "OnMenuShow");

        public static GamepadUser get_m_CurrentGamepadUser(this FrontendRootMenu instance)
        {
            return (GamepadUser)fieldInfo_m_CurrentGamepadUser.GetValue(instance);
        }

        public static void OnMenuShow(this FrontendRootMenu instance, BaseMenuBehaviour menu)
        {
            methodInfo_OnMenuShow.Invoke(instance, new object[] { menu });
        }

        public static void OnMenuHide(this FrontendRootMenu instance, BaseMenuBehaviour menu)
        {
            methodInfo_OnMenuHide.Invoke(instance, new object[] { menu });
        }
    }

    public static class FrontendOptionsMenuExtension
    {
        static readonly FieldInfo fieldInfo_m_ConsoleTopSelectable = AccessTools.Field(typeof(FrontendOptionsMenu), "m_ConsoleTopSelectable");
        static readonly FieldInfo fieldInfo_m_SyncOptions = AccessTools.Field(typeof(FrontendOptionsMenu), "m_SyncOptions");
        static readonly FieldInfo fieldInfo_m_VersionString = AccessTools.Field(typeof(FrontendOptionsMenu), "m_VersionString");

        public static void set_m_VersionString(this FrontendOptionsMenu instance, T17Text text)
        {
            fieldInfo_m_VersionString.SetValue(instance, text);
        }

        public static void set_m_ConsoleTopSelectable(this FrontendOptionsMenu instance, Selectable selectable)
        {
            fieldInfo_m_ConsoleTopSelectable.SetValue(instance, selectable);
        }

        public static ISyncUIWithOption[] get_m_SyncOptions(this FrontendOptionsMenu instance)
        {
            return (ISyncUIWithOption[])fieldInfo_m_SyncOptions.GetValue(instance);
        }

        public static void set_m_SyncOptions(this FrontendOptionsMenu instance, ISyncUIWithOption[] options)
        {
            fieldInfo_m_SyncOptions.SetValue(instance, options);
        }
    }

    public static class BaseUIOptionExtension
    {
        //static readonly FieldInfo fieldInfo_m_OptionType = AccessTools.Field(typeof(BaseUIOption<INameListOption>), "m_OptionType");
        //static readonly FieldInfo fieldInfo_m_Option = AccessTools.Field(typeof(BaseUIOption<INameListOption>), "m_Option");

        //public static void set_m_OptionType(this BaseUIOption<INameListOption> instance, OptionsData.OptionType type)
        //{
        //    fieldInfo_m_OptionType.SetValue(instance, type);
        //}

        //public static void set_m_Option(this BaseUIOption<INameListOption> instance, IOption option)
        //{
        //    fieldInfo_m_Option.SetValue(instance, option);
        //}

        static readonly FieldInfo fieldInfo_m_OptionType = AccessTools.Field(typeof(BaseUIOption<IOption>), "m_OptionType");
        static readonly FieldInfo fieldInfo_m_Option = AccessTools.Field(typeof(BaseUIOption<IOption>), "m_Option");

        public static void set_m_OptionType(this BaseUIOption<IOption> instance, OptionsData.OptionType type)
        {
            fieldInfo_m_OptionType.SetValue(instance, type);
        }

        public static void set_m_Option(this BaseUIOption<IOption> instance, IOption option)
        {
            fieldInfo_m_Option.SetValue(instance, option);
        }
    }
}
