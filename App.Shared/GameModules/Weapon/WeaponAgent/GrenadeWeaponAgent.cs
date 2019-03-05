using App.Shared.Util;
using com.wd.free.@event;
using com.wd.free.para;
using Core;
using Core.EntityComponent;
using Core.Free;

using System;

namespace App.Shared.GameModules.Weapon
{
    /// <summary>
    /// Defines the <see cref="GrenadeWeaponAgent" />
    /// </summary>
    [WeaponSpecies(EWeaponSlotType.ThrowingWeapon)]
    internal class GrenadeWeaponAgent : WeaponBaseAgent
    {
        private GrenadeCacheHelper bagCacheHelper;

        public GrenadeWeaponAgent(Func<EntityKey> in_holdExtractor, Func<EntityKey> in_emptyExtractor, EWeaponSlotType slot, GrenadeCacheHelper grenadeHelper) : base(in_holdExtractor, in_emptyExtractor, slot, grenadeHelper)
        {
            bagCacheHelper = grenadeHelper;
        }
        protected override WeaponEntity Entity
        {
            get { return IsValid() ? bagCacheHelper.GetGrenadeEntity():base.Entity; }
        }
        internal override EntityKey WeaponKey { get { return IsValid() ? bagCacheHelper.GetGrenadeEntity().entityKey.Value:emptyKeyExtractor(); } }
        public override bool IsValid()
        {
            return bagCacheHelper.GetGrenadeEntity().weaponBasicData.ConfigId > 0;
        }
        ///need auto stuff
        public override bool ExpendWeapon()
        {
            var expendId = ConfigId;
            if (expendId < 1) return false;
            bagCacheHelper.RemoveCache(expendId);
            if (!SharedConfig.IsOffline)
                bagCacheHelper.SendFreeTrigger(expendId);
            ReleaseWeapon();
            return true;
        }

        public override int FindNextWeapon(bool autoStuff)
        {
            return bagCacheHelper.FindUsable(autoStuff);
        }

        public override void ReleaseWeapon()
        {
            if (IsValid())
            {
                var grenadeEntity = bagCacheHelper.GetGrenadeEntity();
                grenadeEntity.weaponBasicData.Reset();
                grenadeEntity.weaponRuntimeData.Reset();
            }
        }

        /// <summary>
        /// 手雷武器替换操作：当前ConfigId必须已存在于库存，将手雷ENity替换 为当前configId
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="orient"></param>
        /// <param name="refreshParams"></param>
        /// <returns></returns>
        public override WeaponEntity ReplaceWeapon(EntityKey Owner, WeaponScanStruct orient, ref WeaponPartsRefreshStruct refreshParams)
        {
            if (bagCacheHelper.ShowCount(orient.ConfigId) == 0) return null;
            refreshParams.lastWeaponKey = WeaponKey;
            ReleaseWeapon();
            bagCacheHelper.SetCurr(orient.ConfigId);
            WeaponPartsStruct parts = orient.CreateParts();
            refreshParams.weaponInfo = orient;
            refreshParams.slot = handledSlot;
            refreshParams.oldParts = new WeaponPartsStruct();
            refreshParams.newParts = parts;
            refreshParams.armInPackage = true;
            return bagCacheHelper.GetGrenadeEntity();
        }
    }
}
