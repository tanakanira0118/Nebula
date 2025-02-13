﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.NeutralRoles
{
    public class Jackal : Role
    {
        static public Color RoleColor = new Color(0f, 162f/255f, 211f/255f);

        static private CustomButton killButton;
        static private CustomButton sidekickButton;

        static public Module.CustomOption CanCreateSidekickOption;
        static public Module.CustomOption KillCoolDownOption;

        private Sprite sidekickButtonSprite = null;
        public Sprite getSidekickButtonSprite()
        {
            if (sidekickButtonSprite) return sidekickButtonSprite;
            sidekickButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SidekickButton.png", 115f);
            return sidekickButtonSprite;
        }

        public int jackalDataId { get; private set; }
        public int leftSidekickDataId { get; private set; }

        public override void LoadOptionData()
        {
            CanCreateSidekickOption = CreateOption(Color.white, "canCreateSidekick", true);
            KillCoolDownOption = CreateOption(Color.white, "killCoolDown", 20f, 10f, 60f, 2.5f);
            KillCoolDownOption.suffix = "second";
        }

        public override IEnumerable<Assignable> GetFollowRoles()
        {
            yield return Roles.Sidekick;
        }

        public override void MyPlayerControlUpdate()
        {
            int jackalId = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].GetRoleData(jackalDataId);

            Game.MyPlayerData data = Game.GameData.data.myData;

            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(
                (player) => { 
                    if(player.Object.inVent)return false;
                    if (player.GetModData().role == Roles.Sidekick)
                    {
                        return player.GetModData().GetRoleData(jackalDataId) != jackalId;
                    }
                    else if (player.GetModData().HasExtraRole(Roles.SecondarySidekick))
                    {
                        return player.GetModData().GetExtraRoleData(Roles.SecondarySidekick) != (ulong)jackalId;
                    }
                    return true;
                });

            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.GetModData().SetRoleData(jackalDataId,__instance.PlayerId);
            __instance.GetModData().SetRoleData(leftSidekickDataId, CanCreateSidekickOption.getBool() ? 1 : 0);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (killButton != null)
            {
                killButton.Destroy();
            }
            killButton = new CustomButton(
                () =>
                {
                    Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, Game.PlayerData.PlayerStatus.Dead, true);

                    killButton.Timer = killButton.MaxTimer;
                    Game.GameData.data.myData.currentTarget = null;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.Q
            ).SetTimer(10f);
            killButton.MaxTimer = KillCoolDownOption.getFloat();

            if (sidekickButton != null)
            {
                sidekickButton.Destroy();
            }
            sidekickButton = new CustomButton(
                () =>
                {
                    //Sidekick生成
                    int jackalId = PlayerControl.LocalPlayer.GetModData().GetRoleData(jackalDataId);
                    RPCEventInvoker.CreateSidekick(Game.GameData.data.myData.currentTarget.PlayerId, (byte)jackalId);
                    RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftSidekickDataId, -1);

                    Game.GameData.data.myData.currentTarget = null;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetRoleData(leftSidekickDataId)>0; },
                () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
                () => { sidekickButton.Timer = sidekickButton.MaxTimer; },
                getSidekickButtonSprite(),
                new Vector3(0f, -0.06f, 0),
                __instance,
                KeyCode.F,
                true
            );
            sidekickButton.MaxTimer = 20;
        }

        public override void ButtonActivate()
        {
            killButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            killButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }
        }

        public override void OnDied(byte playerId)
        {
            //SidekickをJackalに昇格

            //対象のJackalID
            int jackalId = Game.GameData.data.players[playerId].GetRoleData(jackalDataId);

            foreach (Game.PlayerData player in Game.GameData.data.players.Values)
            {
                if (Sidekick.SidekickTakeOverOriginalRoleOption.getBool())
                {
                    //Jackalに変化できるプレイヤーを抽出

                    if (player.role.id != Roles.Sidekick.id) continue;
                    if (player.GetRoleData(jackalDataId) != jackalId) continue;
                }
                else
                {
                    //プレイヤーを抽出し、追加役職としてのSidekickを除去

                    if (!player.HasExtraRole(Roles.SecondarySidekick)) continue;
                    if (player.GetExtraRoleData(Roles.SecondarySidekick)!= (ulong)jackalId) continue;

                    RPCEvents.UnsetExtraRole(Roles.SecondarySidekick, player.id);
                }

                RPCEvents.ImmediatelyChangeRole(player.id, id);
                RPCEvents.UpdateRoleData(player.id, jackalDataId, jackalId);
                RPCEvents.UpdateRoleData(player.id, leftSidekickDataId, Sidekick.SidekickCanCreateSidekickOption.getBool() ? 1 : 0);
            }
        }

        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (PlayerControl.LocalPlayer.GetModData().role == Roles.Sidekick || PlayerControl.LocalPlayer.GetModData().role == Roles.Jackal)
            {
                if(PlayerControl.LocalPlayer.GetModData().GetRoleData(jackalDataId)== Helpers.playerById(playerId).GetModData().GetRoleData(jackalDataId))
                {
                    displayColor = RoleColor;
                }
            }else if (PlayerControl.LocalPlayer.GetModData().HasExtraRole(Roles.SecondarySidekick))
            {
                if (PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(Roles.SecondarySidekick) == (ulong)Helpers.playerById(playerId).GetModData().GetRoleData(jackalDataId))
                {
                    displayColor = RoleColor;
                }
            }
        }

        public Jackal()
            : base("Jackal", "jackal", RoleColor, RoleCategory.Neutral, Side.Jackal, Side.Jackal,
                 new HashSet<Side>() { Side.Jackal }, new HashSet<Side>() { Side.Jackal },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JackalWin },
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            killButton = null;
            jackalDataId = Game.GameData.RegisterRoleDataId("jackal.identifier");
            leftSidekickDataId = Game.GameData.RegisterRoleDataId("jackal.leftSidekick");
        }
    }
}
