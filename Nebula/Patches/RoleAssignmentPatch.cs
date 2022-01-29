﻿using HarmonyLib;
using Hazel;
using Nebula.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using static Nebula.NebulaPlugin;

namespace Nebula.Patches
{

    [HarmonyPatch(typeof(RoleOptionsData), nameof(RoleOptionsData.GetNumPerGame))]
    class RoleOptionsDataGetNumPerGamePatch
    {
        public static void Postfix(ref int __result)
        {
            //バニラロールの無効化設定
            if (CustomOptionHolder.activateRoles.getBool()) __result = 0;
        }
    }

    public class AssignRoles
    {
        public class RoleAllocation
        {
            public Role role { get; }
            public int expected { get; }

            public RoleAllocation(Role role,int expected)
            {
                this.role = role;
                this.expected = expected;
            }
        }

        public class CategoryData
        {
            int roles { get; }
            List<Role> firstRoles { get; }
            List<RoleAllocation> secondaryRoles { get; }

            public CategoryData(int min, int max, RoleCategory category)
            {
                this.roles=(min < max) ? NebulaPlugin.rnd.Next(min, max) : max;
                this.firstRoles = new List<Role>();
                this.secondaryRoles = new List<RoleAllocation>();

                foreach (Role role in Roles.Roles.AllRoles)
                {
                    //対象外のロールと非表示ロールはスキップする
                    if (role.category != category)
                    {
                        continue;
                    }
                    if (role.IsHideRole)
                    {
                        continue;
                    }

                    //ロールの湧き数
                    int roleCount = role.FixedRoleCount ? role.GetCustomRoleCount() : (int)role.RoleCountOption.getFloat();

                    if (role.RoleChanceOption.getSelection() < 10)
                    {
                        //ランダムロール
                        for (int i = 0; i < roleCount; i++)
                        {
                            secondaryRoles.Add(new RoleAllocation(role, (int)role.RoleChanceOption.getSelection()));
                        }
                    }
                    else
                    {
                        //100%ロール
                        for (int i = 0; i < roleCount; i++)
                        {
                            firstRoles.Add(role);
                        }
                    }
                }
            }

            public void RegisterRoleChance(RoleAllocation allocation)
            {
                if (allocation.expected < 10)
                {
                    secondaryRoles.Add(allocation);
                }
                else
                {
                    firstRoles.Add(allocation.role);
                }
            }

            public void Assign(List<PlayerControl> players)
            {
                int left = roles;

                int rand;

                //割り当てられるだけ100%ロールを割り当てる
                while ((left > 0) && (firstRoles.Count > 0)&&(players.Count>0))
                {
                    rand = NebulaPlugin.rnd.Next(firstRoles.Count);
                    RoleAssignmentPatch.setRoleToRandomPlayer(firstRoles[rand], players, true);
                    firstRoles.RemoveAt(rand);
                    left--;
                }

                //確率で付与されるロールを割り当てる
                int sum;
                while ((left > 0) && (secondaryRoles.Count > 0)&& (players.Count>0))
                {
                    sum = 0;
                    foreach(RoleAllocation allocation in secondaryRoles)
                    {
                        sum+=allocation.expected;
                    }
                    rand= NebulaPlugin.rnd.Next(sum);

                    if (sum == 0)
                    {
                        break;
                    }

                    for (int i=0; i< secondaryRoles.Count;i++)
                    {
                        if (secondaryRoles[i].expected > sum)
                        {
                            RoleAssignmentPatch.setRoleToRandomPlayer(secondaryRoles[i].role, players, true);
                            secondaryRoles.RemoveAt(i);
                            left--;
                            sum = 0;
                            break;
                        }
                        sum -= secondaryRoles[i].expected;
                    }

                    if (sum != 0)
                    {
                        break;
                    }
                }
            }

            //そのロールは排出される可能性があるかどうか
            public bool Contains(Role role)
            {
                if (firstRoles.Contains(role)) return true;
                if (secondaryRoles.Any((allocation) => { return allocation.role == role; })) return true;
                return false;
            }

            public void Remove(Role role)
            {
                firstRoles.RemoveAll((r) => { return r == role; });
                secondaryRoles.RemoveAll((allocation) => { return allocation.role == role; });
            }
        }

        public CategoryData neutralData { get; }
        public CategoryData crewmateData { get; }
        public CategoryData impostorData { get; }

        public AssignRoles(int crewmates, int impostors)
        {
            //カテゴリごとの人数決定とロール割り当て
            int min, max;

            min = (int)CustomOptionHolder.crewmateRolesCountMin.getFloat();
            max = (int)CustomOptionHolder.crewmateRolesCountMax.getFloat();
            crewmateData = new CategoryData(min,max,RoleCategory.Crewmate);

            min = (int)CustomOptionHolder.impostorRolesCountMin.getFloat();
            max = (int)CustomOptionHolder.impostorRolesCountMax.getFloat();
            impostorData = new CategoryData(min, max, RoleCategory.Impostor);

            min = (int)CustomOptionHolder.neutralRolesCountMin.getFloat();
            max = (int)CustomOptionHolder.neutralRolesCountMax.getFloat();
            if (crewmates - max < impostors + 1)
            {
                max = crewmates - impostors - 1;
            }
            neutralData = new CategoryData(min, max, RoleCategory.Neutral);

            //ComplexRoleの割り当て
            RoleAllocation[] allocations;
            foreach(Role role in Roles.Roles.AllRoles)
            {
                if (role.category != RoleCategory.Complex) continue;

                allocations = role.GetComplexAllocations();
                if (allocations == null)  continue;
                foreach (RoleAllocation allocation in allocations)
                {
                    if (allocation.role.category == RoleCategory.Crewmate)
                    {
                        crewmateData.RegisterRoleChance(allocation);
                    }else if (allocation.role.category == RoleCategory.Impostor)
                    {
                        impostorData.RegisterRoleChance(allocation);
                    }
                    else if (allocation.role.category == RoleCategory.Neutral)
                    {
                        neutralData.RegisterRoleChance(allocation);
                    }
                }
            }

            //排他的割り当て
            List<Module.ExclusiveAssignment> exclusiveAssignmentList = new List<Module.ExclusiveAssignment>();
            CustomOptionHolder.AddExclusiveAssignment(ref exclusiveAssignmentList);
            
            foreach(var assignment in exclusiveAssignmentList)
            {
                assignment.ExclusiveAssign(this);
            }

        }

        public bool Contains(Role role)
        {
            switch (role.category)
            {
                case RoleCategory.Crewmate:
                    return crewmateData.Contains(role);
                    break;
                case RoleCategory.Impostor:
                    return impostorData.Contains(role);
                    break;
                case RoleCategory.Neutral:
                    return neutralData.Contains(role);
                    break;
            }
            return false;
        }

        public bool FuzzyContains(Role role)
        {
            if (Contains(role)) return true;

            foreach(Role fuzzy in role.GetImplicateRoles())
            {
                if (Contains(fuzzy)) return true;
            }
            return false;
        }

        public void RemoveRole(Role role)
        {
            switch (role.category)
            {
                case RoleCategory.Crewmate:
                    crewmateData.Remove(role);
                    break;
                case RoleCategory.Impostor:
                    impostorData.Remove(role);
                    break;
                case RoleCategory.Neutral:
                    neutralData.Remove(role);
                    break;
            }
        }
        public void FuzzyRemoveRole(Role role)
        {
            RemoveRole(role);

            foreach (Role fuzzy in role.GetImplicateRoles())
            {
                RemoveRole(fuzzy);
            }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class RoleAssignmentPatch
    {
        public static void Postfix()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ResetVaribles, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.ResetVaribles();




            if (!DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                if (!CustomOptionHolder.activateRoles.getBool())
                {
                    //ModRoleが無効化されているなら標準ロールを割り当てる
                    assignDefaultRoles();
                }
                else
                {
                    assignRoles();
                }
            }

        }

        private static void assignRoles()
        {
            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            crewmates.RemoveAll(x => x.Data.Role.IsImpostor);
            List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            impostors.RemoveAll(x => !x.Data.Role.IsImpostor);

            /* ロールの割り当て */
            AssignRoles roleData = new AssignRoles(crewmates.Count, impostors.Count);

            roleData.neutralData.Assign(crewmates);
            roleData.crewmateData.Assign(crewmates);
            roleData.impostorData.Assign(impostors);

            //余ったプレイヤーは標準ロールを割り当てる
            while (crewmates.Count > 0)
            {
                setRoleToRandomPlayer(Roles.Roles.Crewmate, crewmates, true);
            }
            while (impostors.Count > 0)
            {
                setRoleToRandomPlayer(Roles.Roles.Impostor, impostors, true);
            }

            /* ExtraRoleの割り当て */
            byte currentPriority = Byte.MinValue;
            byte nextPriority = Byte.MaxValue;
            do
            {
                nextPriority = Byte.MaxValue;

                foreach (ExtraRole role in Roles.Roles.AllExtraRoles)
                {
                    if (role.assignmentPriority == currentPriority)
                    {
                        //ロールを割り当てる
                        role.Assignment(Game.GameData.data);
                    }
                    else if (role.assignmentPriority > currentPriority && role.assignmentPriority < nextPriority)
                    {
                        //次に割り当てる優先度を決定する
                        nextPriority = role.assignmentPriority;
                    }
                }

                currentPriority = nextPriority;
            } while (currentPriority != Byte.MaxValue);
        }

        //ModRoleが有効でない場合標準ロールを割り当てます
        private static void assignDefaultRoles()
        {
            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            crewmates.RemoveAll(x => x.Data.Role.IsImpostor);
            List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            impostors.RemoveAll(x => !x.Data.Role.IsImpostor);

            while (crewmates.Count > 0)
            {
                setRoleToRandomPlayer(Roles.Roles.Crewmate, crewmates, true);

            }
            while (impostors.Count > 0)
            {
                setRoleToRandomPlayer(Roles.Roles.Impostor, impostors, true);
            }
        }

        public static byte setRoleToRandomPlayer(Role role, List<PlayerControl> playerList,bool removePlayerFlag)
        {
            if (playerList.Count == 0)
            {
                return 0;
            }

            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            if (removePlayerFlag)
            {
                playerList.RemoveAt(index);
            }

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRole, Hazel.SendOption.Reliable, -1);
            writer.Write(role.id);
            writer.Write(playerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.SetRole(role,playerId);

            return playerId;
        }
    }
}
