﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Provocateur : Role
    {
        static public Color RoleColor = new Color(112f / 255f, 255f / 255f, 89f / 255f);


        private CustomButton embroilButton;

        private Module.CustomOption embroilCoolDownOption;
        private Module.CustomOption embroilCoolDownAdditionOption;
        private Module.CustomOption embroilDurationOption;

        private Sprite buttonSprite = null;

        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.EmbroilButton.png", 115f);
            return buttonSprite;
        }

        public override void OnMurdered(byte murderId)
        {
            //相手も殺す
            if (PlayerControl.LocalPlayer.PlayerId == murderId) return;
            if (Helpers.playerById(murderId).Data.IsDead) return;
            if (!embroilButton.isEffectActive) return;
            RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, murderId, Game.PlayerData.PlayerStatus.Embroiled.Id, false);
        }

        public override void OnExiledPre(byte[] voters)
        {
            if (voters.Length == 0) return;

            //ランダムに相手を選んで追放する
            RPCEventInvoker.UncheckedExilePlayer(voters[NebulaPlugin.rnd.Next(voters.Length)],Game.PlayerData.PlayerStatus.Embroiled.Id);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            base.ButtonInitialize(__instance);

            if (embroilButton != null)
            {
                embroilButton.Destroy();
            }
            embroilButton = new CustomButton(
                () => { },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () =>
                {
                    return PlayerControl.LocalPlayer.CanMove;
                },
                () =>
                {
                    embroilButton.Timer = embroilButton.MaxTimer;
                    embroilButton.isEffectActive = false;
                },
                getButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F,
                true,
                embroilDurationOption.getFloat(),
                () =>
                {
                    embroilButton.MaxTimer += embroilCoolDownAdditionOption.getFloat();
                    embroilButton.Timer = embroilButton.MaxTimer;
                }
            );
            embroilButton.MaxTimer = embroilCoolDownOption.getFloat();
        }

        public override void ButtonActivate()
        {
            base.ButtonActivate();

            embroilButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            base.ButtonDeactivate();

            embroilButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (embroilButton != null)
            {
                embroilButton.Destroy();
                embroilButton = null;
            }
        }

        public override void LoadOptionData()
        {
            embroilCoolDownOption = CreateOption(Color.white, "embroilCoolDown", 25f, 10f, 60f, 5f);
            embroilCoolDownOption.suffix = "second";

            embroilCoolDownAdditionOption = CreateOption(Color.white, "embroilCoolDownAddition", 10f, 0f, 60f, 2.5f);
            embroilCoolDownAdditionOption.suffix = "second";

            embroilDurationOption = CreateOption(Color.white, "embroilDuration", 5f, 2f, 20f, 1f);
            embroilDurationOption.suffix = "second";
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Spy);
            RelatedRoles.Add(Roles.Madmate);
        }

        public Provocateur()
            : base("Provocateur", "provocateur", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
        }
    }
}
