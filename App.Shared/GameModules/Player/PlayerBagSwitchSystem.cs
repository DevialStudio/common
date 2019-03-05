using App.Shared.GameModules.Weapon;
using Core.GameModule.Interface;
using Core.GameModule.System;
using Core.Prediction.UserPrediction.Cmd;

namespace App.Shared.GameModules.Player
{
    public class PlayerBagSwitchSystem : IUserCmdExecuteSystem
    {
        private ICommonSessionObjects _commonSessionObjects;
        public PlayerBagSwitchSystem(ICommonSessionObjects commonSessionObjects)
        {
            _commonSessionObjects = commonSessionObjects;
        }

        public void ExecuteUserCmd(IUserCmdOwner owner, IUserCmd cmd)
        {
            
            if (cmd.BagIndex > 0)
            {
                var player = owner.OwnerEntity as PlayerEntity;
                if(player.WeaponController().CanSwitchWeaponBag)
                {
                    player.WeaponController().SwitchBag();
                }
            //    var bags = player.playerInfo.WeaponBags;
            }
        }
    }
}
