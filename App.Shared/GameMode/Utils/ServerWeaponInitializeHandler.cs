using App.Shared.GameModules.Weapon;
using Assets.Utils.Configuration;
using Assets.XmlConfig;
using Core;
using Core.Room;
using Core.Utils;
using System;
using System.Collections.Generic;
using Utils.Configuration;
using Utils.Singleton;

namespace App.Shared.GameMode
{
    /// <summary>
    /// Defines the <see cref="ServerWeaponInitHandler" />
    /// </summary>
    public class ServerWeaponInitHandler
    {
        /// <summary>
        /// Defines the <see cref="OverrideBagTaticsCacheData" />
        /// </summary>
        private class OverrideBagTaticsCacheData
        {
            private readonly PlayerWeaponBagData _playerWeaponBagData = new PlayerWeaponBagData();

            public PlayerWeaponBagData CombineOverridedBagData(IPlayerWeaponSharedGetter getter, PlayerWeaponBagData playerWeaponBagData)
            {
                playerWeaponBagData.CopyTo(_playerWeaponBagData);
                if (getter.OverrideBagTactic < 1)
                {
                    return _playerWeaponBagData;
                }
                bool replace = false;
                foreach (var weapon in playerWeaponBagData.weaponList)
                {
                    var slot = PlayerWeaponBagData.Index2Slot(weapon.Index);
                    if (slot == EWeaponSlotType.TacticWeapon)
                    {
                        weapon.WeaponTplId = getter.OverrideBagTactic;
                        replace = true;
                    }
                }
                if (!replace)
                {
                    _playerWeaponBagData.weaponList.Add(new PlayerWeaponData
                    {
                        Index = PlayerWeaponBagData.Slot2Index(EWeaponSlotType.TacticWeapon),
                        WeaponTplId = getter.OverrideBagTactic,
                    });
                }
                return _playerWeaponBagData;
            }
        }

        private readonly OverrideBagTaticsCacheData BagTaticsCache = new OverrideBagTaticsCacheData();



        private void TrashOldWeapons(PlayerEntity player)
        {
            var controller = player.WeaponController();
            var bagSetCmp = player.FindBagSetComponent();
            for (int i = 0; i < bagSetCmp.UsableLength; i++)
            {
                for (EWeaponSlotType j = EWeaponSlotType.None + 1; j < EWeaponSlotType.Length; j++)
                    controller.DestroyWeapon(j, i);
            }
        }

        public void RecoverPlayerWeapon(PlayerEntity player,List<PlayerWeaponBagData> sortedWeaponList)
        {
            //丢弃武器数据
            TrashOldWeapons(player);
            //重新初始化武器数据
            GenerateInitialWeapons(player, sortedWeaponList);
        }

        private void GenerateInitialWeapons(PlayerEntity player, List<PlayerWeaponBagData> sortedWeaponList)
        {
            for(int i=0;i<sortedWeaponList.Count;i++)
                GenerateInitialWeapons(sortedWeaponList[i], player.WeaponController());
            player.playerWeaponUpdate.UpdateHeldAppearance = true;
             
            //defaultBagFstSlot =processor.PollGetLastSlotType();
            //DebugUtil.MyLog("defaultBagFstSlot:" + defaultBagFstSlot,DebugUtil.DebugColor.Blue);
            //processor.TryArmWeapon(defaultBagFstSlot);
        }

        private EWeaponSlotType defaultBagFstSlot;

        private void GenerateInitialWeapons(PlayerWeaponBagData srcBagData, IPlayerWeaponProcessor controller)
        {
            PlayerWeaponBagData bagData = BagTaticsCache.CombineOverridedBagData(controller, srcBagData);
            var helper = controller.GrenadeHelper;
            helper.ClearCache();

            foreach (var weapon in bagData.weaponList)
            {
                DebugUtil.MyLog("BagIndex:{0}|In:{1}" , DebugUtil.DebugColor.Blue,bagData.BagIndex,weapon.ToString());

                var slot = PlayerWeaponBagData.Index2Slot(weapon.Index);
          
                var weaponAllConfig = SingletonManager.Get<WeaponConfigManagement>().FindConfigById(weapon.WeaponTplId);
                var weaponType = (EWeaponType_Config)weaponAllConfig.NewWeaponCfg.Type;
                if (weaponType != EWeaponType_Config.ThrowWeapon)
                {
                    var orient = WeaponUtil.CreateScan(weapon);
                    orient.Bullet = weaponAllConfig.PropertyCfg.Bullet;
                    orient.ReservedBullet = weaponAllConfig.PropertyCfg.Bulletmax;
                    if (orient.Magazine > 0)
                    {
                        orient.Bullet += SingletonManager.Get<WeaponPartsConfigManager>().GetConfigById(orient.Magazine).Bullet;
                    }
                    orient.ClipSize = orient.Bullet;
                    controller.ReplaceWeaponToSlot(slot, srcBagData.BagIndex, orient);
                    //if(isdefaultBag)
                    //    controller.PickUpWeapon(weaponInfo);
                    //else
                    //    controller.ReplaceWeaponToSlot(slot)
                }
                else
                {
                    controller.GrenadeHelper.AddCache(weapon.WeaponTplId);
                }
            }
        }
    }
}
