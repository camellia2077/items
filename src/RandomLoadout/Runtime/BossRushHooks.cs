using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Dungeonator;
using HarmonyLib;

namespace RandomLoadout
{
    internal static class BossRushHooks
    {
        private static readonly string[] SpecialPatchParameterNames =
        {
            "__instance",
            "__result",
            "__state",
            "__runOriginal",
            "__originalMethod",
            "__args"
        };

        public static BossRushHookInstallReport Install(Harmony harmony, ManualLogSource logger)
        {
            BossRushHookInstallReport report = new BossRushHookInstallReport();
            InstallPostfix(
                harmony,
                report,
                typeof(RoomHandler),
                "HandleBossClearReward",
                Type.EmptyTypes,
                "HandleBossClearRewardPostfix",
                logger);
            InstallPostfix(
                harmony,
                report,
                typeof(RewardPedestal),
                "DoPickup",
                new[] { typeof(PlayerController) },
                "RewardPedestalDoPickupPostfix",
                logger);
            InstallPostfix(
                harmony,
                report,
                typeof(Chest),
                "Open",
                new[] { typeof(PlayerController) },
                "ChestOpenPostfix",
                logger);
            InstallPrefix(
                harmony,
                report,
                typeof(GameManager),
                "DoGameOver",
                new[] { typeof(string) },
                "GameManagerDoGameOverPrefix",
                logger);
            InstallPrefix(
                harmony,
                report,
                typeof(PauseMenuController),
                "HandleExitToMainMenu",
                Type.EmptyTypes,
                "PauseMenuHandleExitToMainMenuPrefix",
                logger);
            return report;
        }

        private static void InstallPrefix(
            Harmony harmony,
            BossRushHookInstallReport report,
            Type targetType,
            string targetMethodName,
            Type[] targetArgumentTypes,
            string patchMethodName,
            ManualLogSource logger)
        {
            Install(
                harmony,
                report,
                targetType,
                targetMethodName,
                targetArgumentTypes,
                patchMethodName,
                true,
                logger);
        }

        private static void InstallPostfix(
            Harmony harmony,
            BossRushHookInstallReport report,
            Type targetType,
            string targetMethodName,
            Type[] targetArgumentTypes,
            string patchMethodName,
            ManualLogSource logger)
        {
            Install(
                harmony,
                report,
                targetType,
                targetMethodName,
                targetArgumentTypes,
                patchMethodName,
                false,
                logger);
        }

        private static void Install(
            Harmony harmony,
            BossRushHookInstallReport report,
            Type targetType,
            string targetMethodName,
            Type[] targetArgumentTypes,
            string patchMethodName,
            bool isPrefix,
            ManualLogSource logger)
        {
            string hookLabel = targetType.Name + "." + targetMethodName + " -> " + patchMethodName;
            MethodInfo targetMethod = AccessTools.Method(targetType, targetMethodName, targetArgumentTypes);
            MethodInfo patchMethod = AccessTools.Method(typeof(BossRushHooks), patchMethodName);
            string validationError = ValidateHook(targetMethod, patchMethod, isPrefix);
            if (!string.IsNullOrEmpty(validationError))
            {
                report.AddSkipped(hookLabel, validationError);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Boss Rush hook skipped: " + hookLabel + ". " + validationError));
                }

                return;
            }

            try
            {
                HarmonyMethod harmonyMethod = new HarmonyMethod(patchMethod);
                if (isPrefix)
                {
                    harmony.Patch(targetMethod, prefix: harmonyMethod);
                }
                else
                {
                    harmony.Patch(targetMethod, postfix: harmonyMethod);
                }

                report.AddApplied(hookLabel);
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Init("Boss Rush hook ready: " + hookLabel));
                }
            }
            catch (Exception ex)
            {
                report.AddSkipped(hookLabel, ex.GetType().Name + ": " + ex.Message);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Boss Rush hook failed: " + hookLabel + ". " + ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static string ValidateHook(MethodInfo targetMethod, MethodInfo patchMethod, bool isPrefix)
        {
            if (targetMethod == null)
            {
                return "Target method was not found.";
            }

            if (patchMethod == null)
            {
                return "Patch method was not found.";
            }

            if (!patchMethod.IsStatic)
            {
                return "Patch method must be static.";
            }

            if (isPrefix && patchMethod.ReturnType != typeof(bool))
            {
                return "Prefix patch must return bool.";
            }

            if (!isPrefix && patchMethod.ReturnType != typeof(void))
            {
                return "Postfix patch must return void.";
            }

            ParameterInfo[] targetParameters = targetMethod.GetParameters();
            ParameterInfo[] patchParameters = patchMethod.GetParameters();
            for (int index = 0; index < patchParameters.Length; index++)
            {
                ParameterInfo patchParameter = patchParameters[index];
                if (IsSpecialPatchParameterName(patchParameter.Name))
                {
                    continue;
                }

                ParameterInfo targetParameter;
                if (!TryFindTargetParameter(targetParameters, patchParameter.Name, out targetParameter))
                {
                    return "Patch parameter \"" + patchParameter.Name + "\" does not match any target parameter.";
                }

                if (patchParameter.ParameterType != targetParameter.ParameterType)
                {
                    return "Patch parameter \"" + patchParameter.Name + "\" has type " +
                           patchParameter.ParameterType.FullName +
                           " but target expects " +
                           targetParameter.ParameterType.FullName +
                           ".";
                }
            }

            return null;
        }

        private static bool IsSpecialPatchParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            for (int index = 0; index < SpecialPatchParameterNames.Length; index++)
            {
                if (string.Equals(SpecialPatchParameterNames[index], parameterName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindTargetParameter(ParameterInfo[] targetParameters, string parameterName, out ParameterInfo match)
        {
            for (int index = 0; index < targetParameters.Length; index++)
            {
                ParameterInfo parameter = targetParameters[index];
                if (string.Equals(parameter.Name, parameterName, StringComparison.Ordinal))
                {
                    match = parameter;
                    return true;
                }
            }

            match = null;
            return false;
        }

        private static void HandleBossClearRewardPostfix(RoomHandler __instance)
        {
            BossRushService current = BossRushService.Instance;
            if (current != null)
            {
                current.NotifyBossRewardSpawned(__instance);
            }
        }

        private static void RewardPedestalDoPickupPostfix(PlayerController player)
        {
            BossRushService current = BossRushService.Instance;
            if (current != null)
            {
                current.NotifyRewardClaimed(player);
            }
        }

        private static void ChestOpenPostfix(PlayerController player)
        {
            BossRushService current = BossRushService.Instance;
            if (current != null)
            {
                current.NotifyRewardClaimed(player);
            }
        }

        private static bool GameManagerDoGameOverPrefix(string gameOverSource)
        {
            BossRushService current = BossRushService.Instance;
            return current == null || !current.TryHandleGameOver(gameOverSource);
        }

        private static bool PauseMenuHandleExitToMainMenuPrefix()
        {
            BossRushService current = BossRushService.Instance;
            return current == null || !current.TryHandlePauseMenuExitRequest();
        }
    }

    internal sealed class BossRushHookInstallReport
    {
        private readonly List<string> _appliedHooks = new List<string>();
        private readonly List<string> _skippedHooks = new List<string>();

        public int AppliedCount
        {
            get { return _appliedHooks.Count; }
        }

        public int SkippedCount
        {
            get { return _skippedHooks.Count; }
        }

        public bool HasSkippedHooks
        {
            get { return _skippedHooks.Count > 0; }
        }

        public string[] AppliedHooks
        {
            get { return _appliedHooks.ToArray(); }
        }

        public string[] SkippedHooks
        {
            get { return _skippedHooks.ToArray(); }
        }

        public void AddApplied(string hookLabel)
        {
            _appliedHooks.Add(hookLabel);
        }

        public void AddSkipped(string hookLabel, string reason)
        {
            _skippedHooks.Add(hookLabel + " [" + reason + "]");
        }
    }
}
