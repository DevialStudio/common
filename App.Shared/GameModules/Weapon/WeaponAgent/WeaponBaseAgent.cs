using App.Shared.Components.Weapon;
using Assets.App.Shared.EntityFactory;
using Assets.Utils.Configuration;
using Core;
using Core.Configuration;
using Core.EntityComponent;

using System;
using Utils.Singleton;
using WeaponConfigNs;

namespace App.Shared.GameModules.Weapon
{
    /// <summary>
    /// Defines the <see cref="WeaponBaseAgent" />
    /// </summary>
    public abstract class WeaponBaseAgent
    {

        //      public static IPlayerWeaponResourceConfigManager ConfigManager { protected get; set; }

        protected virtual WeaponEntity Entity
        {
            get { return WeaponEntityFactory.GetWeaponEntity(WeaponKey); }
        }
        internal virtual EntityKey WeaponKey { get { return weaponKeyExtractor(); } }

        internal EntityKey EmptyWeaponKey { get { return emptyKeyExtractor(); } }

        protected Func<EntityKey> weaponKeyExtractor;

        protected Func<EntityKey> emptyKeyExtractor;


        protected EWeaponSlotType handledSlot;
       internal WeaponPartsStruct PartsScan { get { return BaseComponent.CreateParts(); } }
        public abstract int FindNextWeapon(bool autoStuff);
        public abstract bool ExpendWeapon();
        public abstract void ReleaseWeapon();
        public abstract WeaponEntity ReplaceWeapon(EntityKey Owner, WeaponScanStruct orient, ref WeaponPartsRefreshStruct refreshParams);

        public WeaponBaseAgent(Func<EntityKey> in_holdExtractor, Func<EntityKey> in_emptyExtractor, EWeaponSlotType slot, GrenadeCacheHelper grenadeHelper)
        {
            weaponKeyExtractor = in_holdExtractor;
            emptyKeyExtractor = in_emptyExtractor;
            handledSlot = slot;
        }

        public virtual bool IsValid()
        {
            return WeaponKey != EmptyWeaponKey && Entity != null;
        }

        /// <summary>
        /// sync from event of playerEntiy.BagSet.WeaponSlot Component 
        /// </summary>
        /// <param name="entityKey"></param>
        //internal void Sync(EntityKey entityKey)
        //{
        //    if (entityKey == EntityKey.Default)
        //        weaponEntity = WeaponUtil.EmptyWeapon;
        //    else
        //        weaponEntity = WeaponEntityFactory.GetWeaponEntity( entityKey);
        //    WeaponConfigAssy = SingletonManager.Get<WeaponConfigManagement>().FindConfigById(ConfigId);
        //}

        internal WeaponEntity GetEntity()
        {
            if (IsValid()) return Entity;
            return null;
        }

        public WeaponBasicDataComponent BaseComponent
        {
            get { return Entity.weaponBasicData; }
        }

        public WeaponRuntimeDataComponent RunTimeComponent
        {
            get { return Entity.weaponRuntimeData; }
        }

        public int FireModeCount
        {
            get
            {
                if (IsValid())
                    return WeaponConfigAssy.FireModeCount;
                return 1;
            }
        }

        public WeaponScanStruct ComponentScan
        {
            get { return Entity.ToWeaponScan(); }
        }

        public int ConfigId
        {
            get { return Entity.weaponBasicData.ConfigId; }
        }

        public bool IsWeaponEmptyReload
        {
            get
            {
                if (!IsValid())
                    return false;
                return SingletonManager.Get<WeaponResourceConfigManager>().IsSpecialType(ConfigId, ESpecialWeaponType.ReloadEmptyAlways);
            }
        }

        public WeaponAllConfigs WeaponConfigAssy
        {
            get{ return SingletonManager.Get<WeaponConfigManagement>().FindConfigById(ConfigId);}
        }


        public bool IsWeaponConfigStuffed(int weaponId)
        {
            if (!IsValid()) return false;
            return Entity.weaponBasicData.ConfigId == weaponId;
        }



        //public void Reset()
        //{
        //    ResetRuntimeData();
        //    ResetParts();
        //}

        public void ResetRuntimeData()
        {

            if (!IsValid())
                return;
            Entity.weaponRuntimeData.Accuracy = 0;
            Entity.weaponRuntimeData.BurstShootCount = 0;
            Entity.weaponRuntimeData.ContinuesShootCount = 0;
            Entity.weaponRuntimeData.ContinuesShootDecreaseNeeded = false;
            Entity.weaponRuntimeData.ContinuesShootDecreaseTimer = 0;
            Entity.weaponRuntimeData.ContinueAttackEndStamp = 0;
            Entity.weaponRuntimeData.ContinueAttackStartStamp = 0;
            Entity.weaponRuntimeData.NextAttackPeriodStamp = 0;
            Entity.weaponRuntimeData.LastBulletDir = UnityEngine.Vector3.zero;
            Entity.weaponRuntimeData.LastFireTime = 0;
            Entity.weaponRuntimeData.LastSpreadX = 0;
            Entity.weaponRuntimeData.LastSpreadY = 0;
        }

        public void ResetParts()
        {
            if (!IsValid())
                return;
            Entity.weaponBasicData.LowerRail = 0;
            Entity.weaponBasicData.UpperRail = 0;
            Entity.weaponBasicData.Stock = 0;
            Entity.weaponBasicData.Magazine = 0;
            Entity.weaponBasicData.Muzzle = 0;
        }
    
        public CommonFireConfig CommonFireCfg { get { return WeaponConfigAssy.S_CommonFireCfg; } }

        public TacticWeaponBehaviorConfig TacticWeaponLogicCfg { get { return WeaponConfigAssy.S_TacticBehvior; } }

        public DefaultFireLogicConfig DefaultFireLogicCfg { get { return WeaponConfigAssy.S_DefaultFireLogicCfg; } }


        public DefaultWeaponBehaviorConfig DefaultWeaponLogicCfg { get { return WeaponConfigAssy.S_DefualtBehavior; } }

        public PistolAccuracyLogicConfig PistolAccuracyLogicCfg { get { return WeaponConfigAssy.S_PistolAccuracyLogicCfg; } }


        public BaseAccuracyLogicConfig BaseAccuracyLogicCfg { get { return WeaponConfigAssy.S_BaseAccuracyLogicCfg; } }


        public FixedSpreadLogicConfig FixedSpreadLogicCfg { get { return WeaponConfigAssy.S_FixedSpreadLogicCfg; } }


        public PistolSpreadLogicConfig PistolSpreadLogicCfg { get { return WeaponConfigAssy.S_PistolSpreadLogicCfg; } }


        public ShotgunSpreadLogicConfig ShotgunSpreadLogicCfg { get { return WeaponConfigAssy.S_ShotgunSpreadLogicCfg; } }


        public RifleSpreadLogicConfig RifleSpreadLogicCfg { get { return WeaponConfigAssy.S_RifleSpreadLogicCfg; } }

        public SniperSpreadLogicConfig SniperSpreadLogicCfg { get { return WeaponConfigAssy.S_SniperSpreadLogicCfg; } }

        public RifleKickbackLogicConfig RifleKickbackLogicCfg { get { return WeaponConfigAssy.S_RifleKickbackLogicCfg; } }

        public FixedKickbackLogicConfig FixedKickbackLogicCfg { get { return WeaponConfigAssy.S_FixedKickbackLogicCfg; } }


        public DefaultFireModeLogicConfig DefaultFireModeLogicCfg { get { return WeaponConfigAssy.S_DefaultFireModeLogicCfg; } }


        public WeaponResConfigItem ResConfig { get { return WeaponConfigAssy.NewWeaponCfg; } }
        public RifleFireCounterConfig RifleFireCounterCfg { get { return WeaponConfigAssy.S_RifleFireCounterCfg; } }

        public BulletConfig BulletCfg { get { return WeaponConfigAssy.S_BulletCfg; } }

        public int MagazineCapacity { get { return CommonFireCfg != null ? MagazineCapacity : 0; } }

        public float BreathFactor { get { return WeaponConfigAssy != null ? WeaponConfigAssy.GetBreathFactor() : 1; } }

        public float ReloadSpeed { get { return WeaponConfigAssy != null ? WeaponConfigAssy.GetReloadSpeed() : 1; } }

        public float BaseSpeed { get { return WeaponConfigAssy != null ? WeaponConfigAssy.S_Speed : DefaultSpeed; } }


        public float DefaultSpeed
        {
            get
            {
                var config = SingletonManager.Get<WeaponConfigManagement>().FindConfigById(WeaponUtil.EmptyHandId);
                return config.S_Speed;
            }
        }

        public float BaseFov { get { return DefaultFireLogicCfg != null ? DefaultFireLogicCfg.Fov : 90; } }


        public bool CanWeaponSight { get { return DefaultFireLogicCfg != null; } }


        public float FallbackOffsetFactor { get { return FixedKickbackLogicCfg != null ? FixedKickbackLogicCfg.FallbackOffsetFactor : 0f; } }

        public float FocusSpeed { get { return WeaponConfigAssy != null ? WeaponConfigAssy.GetFocusSpeed() : 0f; } }

        public bool IsFovModified { get { return DefaultFireLogicCfg != null && DefaultFireLogicCfg.Fov != WeaponConfigAssy.GetGunSightFov(); } }
        public EBulletCaliber Caliber { get { return WeaponConfigAssy != null ? (EBulletCaliber)WeaponConfigAssy.NewWeaponCfg.Caliber: EBulletCaliber.Length; } }

      
        public float GetGameFov(bool InShiftState)
        {
            if (!IsValid() || IsFovModified) return BaseFov;
            if (InShiftState)
            {


                if (ResConfig != null) return ResConfig.ShiftFov;
            }
            return BaseFov;
        }


    }
}
