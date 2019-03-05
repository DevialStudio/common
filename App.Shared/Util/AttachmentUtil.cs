using App.Shared.Components.Player;
using Assets.Utils.Configuration;
using Assets.XmlConfig;
using Core.Appearance;
using Core;
using Core.Utils;

using System.Collections.Generic;
using Utils.Configuration;
using Utils.Singleton;
using Utils.Utils;
using XmlConfig;
using App.Shared.Components.Weapon;
using App.Shared.GameModules.Weapon;

namespace App.Shared.Util
{
    public static class WeaponPartsUtil
    {
        private static readonly LoggerAdapter Logger = new LoggerAdapter(typeof(WeaponPartsUtil));
        private static Dictionary<WeaponPartLocation, int> _attachmentsDic = new Dictionary<WeaponPartLocation, int>(CommonIntEnumEqualityComparer<WeaponPartLocation>.Instance);
        private static Dictionary<WeaponPartLocation, int> _oldAttachmentsDic = new Dictionary<WeaponPartLocation, int>(CommonIntEnumEqualityComparer<WeaponPartLocation>.Instance);

        /// <summary>
        /// 刷新武器的配件显示
        /// </summary>
        /// <param name="appearance"></param>
        /// <param name="weaponId">武器的Id</param>
        /// <param name="attachments">武器的配件信息</param>
        /// <param name="slot">武器的位置</param>
        public static void RefreshWeaponPartModels(ICharacterAppearance appearance, int weaponId, WeaponPartsStruct oldAttachment, WeaponPartsStruct attachments, EWeaponSlotType slot)
        {
            Logger.DebugFormat("RefreshAttachmnetModels {0}, old {1}, new {2}, slot {3}",
                weaponId,
                oldAttachment,
                attachments,
                slot);
            var weaponConfig = SingletonManager.Get<WeaponResourceConfigManager>().GetConfigById(weaponId);
            if (null == weaponConfig)
            {
                return;
            }
            if (!((EWeaponType_Config)weaponConfig.Type).MayHasPart())
            {
                Logger.WarnFormat("weapon type {0} has no attachment by default ", weaponConfig.Type);
                return;
            }
            PrepareDicsForAttach(oldAttachment, attachments);

            var pos = slot.ToWeaponInPackage();

            foreach (var pair in _attachmentsDic)
            {
                if (pair.Value > 0)
                {
                    if (!_oldAttachmentsDic.ContainsKey(pair.Key) || _oldAttachmentsDic[pair.Key] != pair.Value)
                    {
                        appearance.MountAttachment(pos, pair.Key, pair.Value);
                    }
                }
                else
                {
                    if (_oldAttachmentsDic.ContainsKey(pair.Key) && _oldAttachmentsDic[pair.Key] > 0)
                    {
                        appearance.UnmountAttachment(pos, pair.Key);
                    }
                }
            }
        }

        private static void PrepareDicsForAttach(WeaponPartsStruct oldAttachments, WeaponPartsStruct newAttachments)
        {
            GenerateOldAttachmentsDic(oldAttachments);
            GenerateNewAttachmentDic(newAttachments);
        }

        private static void GenerateNewAttachmentDic(WeaponPartsStruct attachments)
        {
            MapAttachmentsToAttachmentDic(attachments, _attachmentsDic);
        }

        private static void GenerateOldAttachmentsDic(WeaponPartsStruct attachments)
        {
            MapAttachmentsToAttachmentDic(attachments, _oldAttachmentsDic);
        }

        private static void MapAttachmentsToAttachmentDic(WeaponPartsStruct attachments, Dictionary<WeaponPartLocation, int> attachmentDic)
        {
            attachmentDic.Clear();
            attachmentDic[WeaponPartLocation.LowRail] = attachments.LowerRail;
            attachmentDic[WeaponPartLocation.Scope] = attachments.UpperRail;
            attachmentDic[WeaponPartLocation.Buttstock] = attachments.Stock;
            attachmentDic[WeaponPartLocation.Muzzle] = attachments.Muzzle;
            attachmentDic[WeaponPartLocation.Magazine] = attachments.Magazine;
        }

    
     
        public static WeaponScanStruct SetWeaponInfoAttachment(WeaponScanStruct weaponInfo, EWeaponPartType type, int id)
        {
            switch (type)
            {
                case EWeaponPartType.LowerRail:
                    weaponInfo.LowerRail = id;
                    break;
                case EWeaponPartType.UpperRail:
                    weaponInfo.UpperRail = id;
                    break;
                case EWeaponPartType.Muzzle:
                    weaponInfo.Muzzle = id;
                    break;
                case EWeaponPartType.Magazine:
                    weaponInfo.Magazine = id;
                    break;
                case EWeaponPartType.Stock:
                    weaponInfo.Stock = id;
                    break;
            }

            return weaponInfo;
        }


   

        public static void ApplyParts(this WeaponBasicDataComponent comp, WeaponPartsStruct attach)
        {
            comp.LowerRail = attach.LowerRail;
            comp.UpperRail = attach.UpperRail;
            comp.Muzzle = attach.Muzzle;
            comp.Magazine = attach.Magazine;
            comp.Stock = attach.Stock;
        }

   
    }
}