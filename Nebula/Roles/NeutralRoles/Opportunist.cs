﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Patches;

namespace Nebula.Roles.NeutralRoles
{
    public class Opportunist : Template.ExemptTasks
    {
        static public Color Color = new Color(106f / 255f, 252f / 255f, 45f / 255f);


        private Module.CustomOption cutTasksOption;

        public override bool CheckWin(PlayerControl player, EndCondition condition)
        {
            if (player.Data.IsDead) return false;
            if (condition == EndCondition.ArsonistWin) return false;
            if (condition == EndCondition.EmpiricWin) return false;
            if (condition == EndCondition.JesterWin) return false;

            if (player.GetModData().Tasks.AllTasks == player.GetModData().Tasks.Completed)
            {
                EndGameManagerSetUpPatch.AddEndText(Language.Language.GetString("role.opportunist.additionalEndText"));
                return true;
            }
            
            return false;
        }

        public Opportunist()
            : base("Opportunist", "opportunist", Color, RoleCategory.Neutral, Side.Opportunist, Side.Opportunist,
                 new HashSet<Side>() { Side.Opportunist }, new HashSet<Side>() { Side.Opportunist },
                 new HashSet<Patches.EndCondition>(),
                 true, true, true, false, false)
        {
            fakeTaskIsExecutable = true;
        }
    }
}