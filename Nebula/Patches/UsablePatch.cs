﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    class KillButtonDoClickPatch
    {
        public static bool Prefix(KillButton __instance)
        {
            if (__instance.isActiveAndEnabled && __instance.currentTarget && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.CanMove)
            {
                Helpers.MurderAttemptResult res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, __instance.currentTarget, Game.PlayerData.PlayerStatus.Dead);
                if (res == Helpers.MurderAttemptResult.BlankKill)
                {
                    PlayerControl.LocalPlayer.killTimer = PlayerControl.GameOptions.KillCooldown;
                }
                __instance.SetTarget(null);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.NextStep))]
    class TaskCompletePatch
    {
        static void Prefix(NormalPlayerTask __instance)
        {
            if (__instance.MaxStep-1 == __instance.TaskStep)
                if (__instance.Owner.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    RPCEventInvoker.CompleteTask(__instance.Owner.PlayerId);
        }
    }

    [HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.Refresh))]
    class SabotageButtonRefreshPatch
    {
        static void Postfix(SabotageButton __instance)
        {
            if (HudManager.Instance==null) return;
            if (Game.GameData.data == null) return;
            if (Game.GameData.data.myData.getGlobalData() == null) return;

            if (!Game.GameData.data.myData.getGlobalData().role.canInvokeSabotage)
            {
                __instance.SetDisabled();
            }
        }
    }

    [HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
    public static class SabotageButtonDoClickPatch
    {
        public static bool Prefix(SabotageButton __instance)
        {
            //インポスターなら特段何もしない
            if (PlayerControl.LocalPlayer.Data.Role.TeamType == RoleTeamTypes.Impostor) return true;

            DestroyableSingleton<HudManager>.Instance.ShowMap((Il2CppSystem.Action<MapBehaviour>)((m) => { m.ShowSabotageMap(); }));
            return false;
        }
    }
}
