﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Comet : Role
    {
        static public Color RoleColor = new Color(121f / 255f, 175f / 255f, 206f / 255f);

        private CustomButton boostButton;

        private Module.CustomOption boostCooldownOption;
        private Module.CustomOption boostDurationOption;
        private Module.CustomOption boostSpeedOption;
        private Module.CustomOption boostLightOption;

        private float lightLevel = 1f;

        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.BoostButton.png", 115f);
            return buttonSprite;
        }

        public override void MyUpdate()
        {
            if (boostButton == null) return;

            if (boostButton.isEffectActive)
                lightLevel += 0.5f * Time.deltaTime;
            else
                lightLevel -= 0.5f * Time.deltaTime;
            lightLevel = Mathf.Lerp(0f, 1f, lightLevel);
        }

        public override void GetLightRadius(ref float radius)
        {
            radius *= Mathf.Lerp(1f, boostLightOption.getFloat(), lightLevel);
        }

        public override void Initialize(PlayerControl __instance)
        {
            lightLevel = 0f;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (boostButton != null)
            {
                boostButton.Destroy();
            }
            boostButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(0,boostDurationOption.getFloat(), boostSpeedOption.getFloat(), false));
                    RPCEventInvoker.EmitAttributeFactor(PlayerControl.LocalPlayer, new Game.PlayerAttributeFactor(Game.PlayerAttribute.Invisible, boostDurationOption.getFloat(), 0, false));
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () =>
                {
                    boostButton.Timer = boostButton.MaxTimer;
                    boostButton.isEffectActive = false;
                    boostButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                getButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                true,
               boostDurationOption.getFloat(),
               () => { boostButton.Timer = boostButton.MaxTimer; },
                false,
                "button.label.blaze"
            );
            boostButton.MaxTimer = boostCooldownOption.getFloat();
        }
    
        public override void ButtonActivate()
        {
            boostButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            boostButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (boostButton != null)
            {
                boostButton.Destroy();
                boostButton = null;
            }
        }

        public override void LoadOptionData()
        {
            boostCooldownOption = CreateOption(Color.white, "boostCoolDown", 20f, 10f, 60f, 5f);
            boostCooldownOption.suffix = "second";

            boostDurationOption = CreateOption(Color.white, "boostDuration", 10f, 5f, 30f, 5f);
            boostDurationOption.suffix = "second";

            boostSpeedOption = CreateOption(Color.white, "boostSpeed", 2f, 1.25f, 3f, 0.25f);
            boostSpeedOption.suffix = "cross";

            boostLightOption = CreateOption(Color.white, "boostVisionRate", 1.5f, 1f, 2f, 0.25f);
            boostLightOption.suffix = "cross";
        }

        public Comet()
            : base("Comet", "comet", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, true, false)
        {
            boostButton = null;
        }
    }
}
