using com.wd.free.action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.wd.free.@event;
using App.Server.GameModules.GamePlay.free.player;
using App.Shared.GameModules.Player;

namespace App.Shared.FreeFramework.Free.player
{
    [Serializable]
    public class PlayerObserveAction : AbstractPlayerAction
    {
        private string observeEnemy;
        private IGameAction noOneAction;
        private string wise;

        public override void DoAction(IEventArgs args)
        {
            FreeData fd = GetPlayer(args);

            bool success = ObservePlayer(args, fd, args.GetBool(observeEnemy), args.GetBool(wise));
            if (!success)
            {
                if (noOneAction != null)
                {
                    noOneAction.Act(args);
                }
            }
        }

        public static bool ObservePlayer(IEventArgs args, FreeData fd, bool observeEnemy, bool wiseFlag)
        {
            bool hasTeamMate = false;
            PlayerEntity revenge = null;
            bool hasEnemy = false;

            foreach (PlayerEntity p in args.GameContext.player.GetInitializedPlayerEntities())
            {
                if (CanOb(fd.Player, p))
                {
                    if (p.playerInfo.Camp == fd.Player.playerInfo.Camp)
                    {
                        hasTeamMate = true;
                    }
                    if (fd.Player.statisticsData.Statistics.RevengeKillerId == p.playerInfo.PlayerId)
                    {
                        revenge = p;
                    }
                    if (p.playerInfo.Camp != fd.Player.playerInfo.Camp)
                    {
                        hasEnemy = true;
                    }
                }
            }

            if (hasTeamMate)
            {
                if (!FindSomeOne(args, fd, true, wiseFlag))
                {
                    fd.Player.gamePlay.CameraEntityId = wiseFlag ? 0 : 999;
                    FindSomeOne(args, fd, true, wiseFlag);
                }

                return true;
            }
            else if (revenge != null && observeEnemy)
            {
                fd.Player.gamePlay.CameraEntityId = revenge.entityKey.Value.EntityId;
                return true;
            }
            else if (hasEnemy && observeEnemy)
            {
                if (!FindSomeOne(args, fd, false, wiseFlag))
                {
                    fd.Player.gamePlay.CameraEntityId = wiseFlag ? 0 : 999;
                    FindSomeOne(args, fd, false, wiseFlag);
                }
                return true;
            }

            return false;
        }

        private static bool FindSomeOne(IEventArgs args, FreeData fd, bool sameTeam, bool wiseFlag)
        {
            foreach (PlayerEntity p in args.GameContext.player.GetInitializedPlayerEntities())
            {
                if (CanOb(fd.Player, p))
                {
                    if (sameTeam && p.playerInfo.Camp == fd.Player.playerInfo.Camp || !sameTeam && p.playerInfo.Camp != fd.Player.playerInfo.Camp)
                    {
                        if (wiseFlag)
                        {
                            if (p.entityKey.Value.EntityId > fd.Player.gamePlay.CameraEntityId)
                            {
                                fd.Player.gamePlay.CameraEntityId = p.entityKey.Value.EntityId;
                                return true;
                            }
                        }
                        else
                        {
                            if (p.entityKey.Value.EntityId < fd.Player.gamePlay.CameraEntityId)
                            {
                                fd.Player.gamePlay.CameraEntityId = p.entityKey.Value.EntityId;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool CanOb(PlayerEntity p, PlayerEntity candidate)
        {
            return p != candidate && !candidate.gamePlay.IsDead();
        }
    }
}
