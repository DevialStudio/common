using App.Shared;
using App.Shared.Components.Weapon;
using Assets.Utils.Configuration;
using Assets.XmlConfig;
using Core;
using Core.CharacterState;
using Core.Configuration;
using Core.EntityComponent;
using Core.Room;
using System.Collections.Generic;
using Utils.Configuration;
using Utils.Singleton;
using WeaponConfigNs;
using XmlConfig;

namespace App.Shared
{
   
    public static class WeaponPartUtil
    {
        public static WeaponPartsStruct CreateParts(this WeaponScanStruct info)
        {
            var result = new WeaponPartsStruct
            {
                LowerRail = info.LowerRail,
                UpperRail = info.UpperRail,
                Magazine = info.Magazine,
                Muzzle = info.Muzzle,
                Stock = info.Stock,
            };
            CombineDefaultParts(ref result, info.ConfigId);
            return result;
        }
        public static WeaponPartsStruct CreateParts(this WeaponBasicDataComponent comp)
        {
            var result = new WeaponPartsStruct
            {
                LowerRail = comp.LowerRail,
                UpperRail = comp.UpperRail,
                Muzzle = comp.Muzzle,
                Stock = comp.Stock,
                Magazine = comp.Magazine,
            };

            CombineDefaultParts(ref result, comp.ConfigId);
            return result;
        }

        public static void CombineDefaultParts(ref WeaponPartsStruct result, int weaponId)
        {
            var defaultParts = SingletonManager.Get<WeaponResourceConfigManager>().GetDefaultWeaponAttachments(weaponId);
            foreach (var part in defaultParts)
            {
                if (part < 1)
                {
                    continue;
                }
                var partCfg = SingletonManager.Get<WeaponPartsConfigManager>().GetConfigById(part);
                switch ((EWeaponPartType)partCfg.Type)
                {
                    case EWeaponPartType.LowerRail:
                        result.LowerRail = result.LowerRail > 0 ? result.LowerRail : part;
                        break;
                    case EWeaponPartType.UpperRail:
                        result.UpperRail = result.UpperRail > 0 ? result.UpperRail : part;
                        break;
                    case EWeaponPartType.Muzzle:
                        result.Muzzle = result.Muzzle > 0 ? result.Muzzle : part;
                        break;
                    case EWeaponPartType.Magazine:
                        result.Magazine = result.Magazine > 0 ? result.Magazine : part;
                        break;
                    case EWeaponPartType.Stock:
                        result.Stock = result.Stock > 0 ? result.Stock : part;
                        break;
                }
            }
        }
        /// <summary>
        /// 根据PartType修改配件信息
        /// </summary>
        /// <param name="attach"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static WeaponPartsStruct ModifyPartItem(WeaponPartsStruct attach, EWeaponPartType type, int id)
        {
            switch (type)
            {
                case EWeaponPartType.LowerRail:
                    attach.LowerRail = id;
                    break;
                case EWeaponPartType.UpperRail:
                    attach.UpperRail = id;
                    break;
                case EWeaponPartType.Muzzle:
                    attach.Muzzle = id;
                    break;
                case EWeaponPartType.Magazine:
                    attach.Magazine = id;
                    break;
                case EWeaponPartType.Stock:
                    attach.Stock = id;
                    break;
            }

            return attach;
        }

    }

    /// <summary>
    /// Defines the <see cref="WeaponUtil" />
    /// </summary>
    public static class WeaponUtil
    {
        public static int EmptyHandId
        {
            get { return SingletonManager.Get<WeaponResourceConfigManager>().EmptyHandId; }
        }
        //public static readonly WeaponEntity EmptyWeapon = new WeaponEntity();
        //public readonly static WeaponRuntimeDataComponent EmptyRun = new WeaponRuntimeDataComponent();

        //public readonly static WeaponBasicDataComponent EmptyWeaponBase = new WeaponBasicDataComponent();
        public static EWeaponSlotType GetEWeaponSlotTypeById(int weaponId)
        {
            var configType = (EWeaponType_Config)SingletonManager.Get<WeaponResourceConfigManager>().GetConfigById(weaponId).Type;
            return configType.ToWeaponSlot();
        }
 
        /// <summary>
        /// 验证WeaponId合法
        /// </summary>
        /// <param name="weaponId"></param>
        /// <returns></returns>
        public static bool VertifyWeaponConfigId(int weaponId)
        {
            WeaponResConfigItem config;
            return VertifyWeaponConfigId(weaponId, out config);
        }

        public static bool VertifyWeaponConfigId(int weaponId, out WeaponResConfigItem config)
        {
            config = null;
            if (weaponId == 0)
                return false;
            config = SingletonManager.Get<WeaponResourceConfigManager>().GetConfigById(weaponId);
            return config != null;
        }

      
        public static List<WeaponBagContainer> CreateEmptyBagContainers()
        {
            var containerSet = new List<WeaponBagContainer>(GlobalConst.WeaponBagMaxCount);
            for (int i = 0; i < GlobalConst.WeaponBagMaxCount; i++)
            {
                containerSet.Add(new WeaponBagContainer());
            }
            return containerSet;
        }

        public static WeaponScanStruct CreateScan(int configId, System.Action<WeaponScanStruct> initFunc)
        {
            WeaponScanStruct val = new WeaponScanStruct();
            val.ConfigId =configId;
            initFunc(val);
            return val;
        }

        public static WeaponScanStruct CreateScan(PlayerWeaponData weaponData)
        {
            WeaponScanStruct val = new WeaponScanStruct();
            val.ConfigId =weaponData.WeaponTplId;
            val.AvatarId = weaponData.WeaponTplId;
            val.UpperRail = weaponData.UpperRail;
            val.LowerRail = weaponData.LowerRail;
            val.Magazine = weaponData.Magazine;
            val.Muzzle = weaponData.Muzzle;
            val.Stock = weaponData.Stock;
            return val;
        }

        //public static WeaponScanStruct CreateScan(Components.SceneObject.WeaponObjectComponent weaponObject)
        //{
        //    WeaponScanStruct val = new WeaponScanStruct();
        //    val.Assign(weaponObject.ConfigId);
        //    val.AvatarId = weaponObject.WeaponAvatarId;
        //    val.UpperRail = weaponObject.UpperRail;
        //    val.LowerRail = weaponObject.LowerRail;
        //    val.Magazine = weaponObject.Magazine;
        //    val.Muzzle = weaponObject.Muzzle;
        //    val.Stock = weaponObject.Stock;
        //    return val;
        //}

        public static WeaponScanStruct CreateScan(int configId)
        {
            WeaponScanStruct val = new WeaponScanStruct();
            val.ConfigId = configId;
            return val;
        }

        public static WeaponScanStruct CreateScan()
        {
            WeaponScanStruct val = new WeaponScanStruct();
            val.ConfigId = EmptyHandId;
            return val;
        }

        public static WeaponScanStruct CreateScan(WeaponEntity entity)
        {
            WeaponScanStruct val = new WeaponScanStruct();
            val.ConfigId = entity.weaponBasicData.ConfigId;
            return val;
        }

        public static bool VertifyEweaponSlotIndex(int index, bool ignoreNone = false)
        {
            return (index > 0 || ignoreNone) && index < (int)EWeaponSlotType.Length;
        }

        public static float GetWeaponDefaultSpeed()
        {
            var config = SingletonManager.Get<WeaponConfigManagement>().FindConfigById(EmptyHandId);
            return config.S_Speed;
        }

        public static List<GrenadeCacheData> CreateEmptyGrenadeCacheArrs(List<int> ids)
        {
            var containerSet = new List<GrenadeCacheData>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                containerSet.Add(new GrenadeCacheData());
                containerSet[i].grenadeId = ids[i];
            }
            return containerSet;
        }

        public static List<int> ForeachFilterGreandeIds()
        {
            var configs = SingletonManager.Get<WeaponResourceConfigManager>().GetConfigs();
            var grenadeConfigIds = new List<int>();
            foreach (var config in configs)
            {
                switch ((EWeaponType_Config)config.Value.Type)
                {
                    case EWeaponType_Config.ThrowWeapon:
                        var subType = (EWeaponSubType)config.Value.SubType;
                        switch (subType)
                        {
                            case EWeaponSubType.BurnBomb:
                            case EWeaponSubType.FlashBomb:
                            case EWeaponSubType.FogBomb:
                            case EWeaponSubType.Grenade:
                                break;
                            default:
                                break;
                        }
                        grenadeConfigIds.Add(config.Value.Id);
                        break;
                }
            }
            return grenadeConfigIds;
        }

        public static int GetRealAttachmentId(int attachId, int weaponId)
        {
            var cfg = SingletonManager.Get<WeaponPartSurvivalConfigManager>().FindConfigBySetId(attachId);
            if (null == cfg)
            {
                return 0;
            }
            for (int i = 0; i < cfg.PartsList.Length; i++)
            {
                if (SingletonManager.Get<WeaponPartsConfigManager>().IsPartMatchWeapon(cfg.PartsList[i], weaponId))
                {
                    return cfg.PartsList[i];
                }
            }
            return 0;
        }

        public static bool IsC4p(int configId)
        {
            return SingletonManager.Get<WeaponResourceConfigManager>().IsC4(configId);
        }

        public static float GetHolsterParam(EWeaponSlotType slot)
        {
            return GetHolsterParam(slot == EWeaponSlotType.SecondaryWeapon);
        }

        public static float GetHolsterParam(bool val)
        {
            return val ?
                 AnimatorParametersHash.Instance.HolsterFromLeftValue :
                 AnimatorParametersHash.Instance.HolsterFromRightValue;
        }

    }
}
