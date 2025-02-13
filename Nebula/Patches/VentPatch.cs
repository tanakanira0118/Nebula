﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    public static class VentEnterPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (pc != PlayerControl.LocalPlayer) return;
            Game.GameData.data.myData.VentDurationTimer = pc.GetModData().role.VentDurationMaxTimer;
            Helpers.RoleAction(pc, (role) => role.OnEnterVent(__instance));
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
    public static class VentExitPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (pc != PlayerControl.LocalPlayer) return;
            Game.GameData.data.myData.VentCoolDownTimer = pc.GetModData().role.VentCoolDownMaxTimer;
            Helpers.RoleAction(pc, (role) => role.OnExitVent(__instance));
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    public static class VentCanUsePatch
    {
        public static bool Prefix(Vent __instance, ref float __result, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            float num = float.MaxValue;
            PlayerControl @object = pc.Object;

            bool roleCouldUse = false;
            if (Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role.VentPermission != Roles.VentPermission.CanNotUse)
                roleCouldUse = !HudManager.Instance.ImpostorVentButton.isCoolingDown;

            var usableDistance = __instance.UsableDistance;
            
            if (__instance.GetVentData()!=null && __instance.GetVentData().Sealed)
            {
                canUse = couldUse = false;
                __result = num;
                return false;
            }


            couldUse = (@object.inVent || roleCouldUse) && !pc.IsDead && (@object.CanMove || @object.inVent);
            canUse = couldUse;

            if (canUse)
            {
                Vector2 truePosition = @object.GetTruePosition();
                Vector3 position = __instance.transform.position;
                num = Vector2.Distance(truePosition, position);

                canUse &= (num <= usableDistance && !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShipOnlyMask, false));

                if (@object.MyPhysics.Animator.Clip == @object.MyPhysics.CurrentAnimationGroup.EnterVentAnim && @object.MyPhysics.Animator.Playing) canUse = false;
            }
            __result = num;

            return false;
        }
    }

    [HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
    class VentButtonDoClickPatch
    {
        static bool Prefix(VentButton __instance)
        {
            // Manually modifying the VentButton to use Vent.Use again in order to trigger the Vent.Use prefix patch
            if (__instance.currentTarget != null) __instance.currentTarget.Use();
            return false;
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
    public static class VentUsePatch
    {
        public static bool Prefix(Vent __instance)
        {
            bool canUse=false;
            bool couldUse;
            bool canMoveInVents;

            __instance.CanUse(PlayerControl.LocalPlayer.Data, out canUse, out couldUse);

            if (Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role.VentPermission != Roles.VentPermission.CanNotUse)
                canUse &= !HudManager.Instance.ImpostorVentButton.isCoolingDown;
            
            canMoveInVents = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role.CanMoveInVents;
            
            if (!canUse) return false; // No need to execute the native method as using is disallowed anyways

            bool isEnter = !PlayerControl.LocalPlayer.inVent;


            if (isEnter)
            {
                PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(__instance.Id);
            }
            else
            {
                PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(__instance.Id);
            }
            __instance.SetButtons(isEnter && canMoveInVents);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class VentButtonVisibilityPatch
    {
        static void Postfix(PlayerControl? __instance)
        {
            if (__instance == null)
            {
                return;
            }

            if (Game.GameData.data == null)
            {
                return;
            }

            if (!Game.GameData.data.players.ContainsKey(__instance.PlayerId))
            {
                return;
            }

            if (!__instance.AmOwner) return;

            var data = Game.GameData.data.players[__instance.PlayerId];
            if (data == null) return;

            var role = data.role;
            var showFlag = HudManager.Instance.ReportButton.isActiveAndEnabled;

            if (__instance.CanMove) Game.GameData.data.myData.VentCoolDownTimer -= Time.deltaTime;
            Game.GameData.data.myData.VentDurationTimer -= Time.deltaTime;

            if (Game.GameData.data.myData.VentCoolDownTimer < 0f) Game.GameData.data.myData.VentCoolDownTimer = 0f;
            if (Game.GameData.data.myData.VentDurationTimer < 0f) Game.GameData.data.myData.VentDurationTimer = 0f;

            if (role.VentPermission != Roles.VentPermission.CanNotUse && showFlag)
            {
                HudManager.Instance.ImpostorVentButton.Show();

                if (!HudManager.Instance.ImpostorVentButton.cooldownTimerText)
                {
                    HudManager.Instance.ImpostorVentButton.cooldownTimerText =
                        UnityEngine.Object.Instantiate(HudManager.Instance.AbilityButton.cooldownTimerText, HudManager.Instance.ImpostorVentButton.transform);
                }

                if (role.VentPermission == Roles.VentPermission.CanUseLimittedVent)
                {
                    if (__instance.inVent)
                    {
                        HudManager.Instance.ImpostorVentButton.cooldownTimerText.text = Mathf.CeilToInt(Game.GameData.data.myData.VentDurationTimer).ToString();
                        HudManager.Instance.ImpostorVentButton.cooldownTimerText.gameObject.SetActive(true);
                    }
                    else
                    {
                        HudManager.Instance.ImpostorVentButton.SetCoolDown(Game.GameData.data.myData.VentCoolDownTimer, role.VentCoolDownMaxTimer);
                    }
                }
                else
                {
                    HudManager.Instance.ImpostorVentButton.SetCoolDown(0f,10f);
                }
            }
            else
                HudManager.Instance.ImpostorVentButton.Hide();
            
            if (role.canInvokeSabotage && showFlag)
                HudManager.Instance.SabotageButton.Show();
            else
                HudManager.Instance.SabotageButton.Hide();

            if(__instance.inVent && role.VentPermission == Roles.VentPermission.CanUseLimittedVent &&
                !(Game.GameData.data.myData.VentDurationTimer > 0f))
            {
                Vent vent = HudManager.Instance.ImpostorVentButton.currentTarget;
                if (!vent.GetVentData().Sealed)
                {
                    __instance.MyPhysics.RpcExitVent(vent.Id);
                    vent.SetButtons(false);
                }
            }
        }
    }
}
