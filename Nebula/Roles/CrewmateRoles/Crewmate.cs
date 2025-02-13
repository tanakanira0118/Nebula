﻿using Nebula.Patches;
using System.Collections.Generic;

namespace Nebula.Roles.CrewmateRoles
{
    public class Crewmate : Role
    {
        public static HashSet<Side> crewmateSideSet = new HashSet<Side>() { Side.Crewmate };
        public static HashSet<EndCondition> crewmateEndSet =
            new HashSet<EndCondition>() { EndCondition.CrewmateWinByTask, EndCondition.CrewmateWinByVote, EndCondition.CrewmateWinDisconnect };


        public override bool CanBeLovers
        {
            get
            {
                return Roles.F_Crewmate.CanBeLovers;
            }
        }

        public override bool CanBeGuesser
        {
            get
            {
                return Roles.F_Crewmate.CanBeGuesser;
            }
        }

        public override bool CanBeDrunk
        {
            get
            {
                return Roles.F_Crewmate.CanBeDrunk;
            }
        }

        public override bool IsGuessableRole { get => Roles.F_Crewmate.isGuessableOption.getBool(); protected set => base.IsGuessableRole = value; }

        public override List<Role> GetImplicateRoles()
        {
            return new List<Role>() { Roles.DamnedCrew };
        }

        public Crewmate()
                : base("Crewmate", "crewmate", Palette.CrewmateBlue, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     crewmateSideSet, crewmateSideSet, crewmateEndSet,
                     false, VentPermission.CanNotUse, false, false, false)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Standard;
        }
    }
}
