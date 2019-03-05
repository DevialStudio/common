using App.Shared.Util;
using Core;

using Utils.Configuration;
using Utils.Singleton;
using Utils.Utils;
using XmlConfig;

namespace App.Shared.GameModules.Weapon
{
    /// <summary>
    /// Defines the <see cref="PlayerWeaponController" />
    /// </summary>
    public partial class PlayerWeaponController
    {
        /// <summary>
        /// API:parts
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool SetWeaponPart(EWeaponSlotType slot, int id)
        {
            var agent = GetWeaponAgent(slot);
            if (!agent.IsValid()) return false;
            WeaponPartsStruct lastParts = agent.BaseComponent.CreateParts();
            int realAttachId = WeaponUtil.GetRealAttachmentId(id, agent.ResConfig.Id);
            bool match = SingletonManager.Get<WeaponPartsConfigManager>().IsPartMatchWeapon(realAttachId, agent.ResConfig.Id);
            if (!match)
                return false;
            var attachments = WeaponPartUtil.ModifyPartItem(
                agent.BaseComponent.CreateParts(),
                SingletonManager.Get<WeaponPartsConfigManager>().GetPartType(realAttachId),
                realAttachId);
            agent.BaseComponent.ApplyParts(attachments);
            if (slot == HeldSlotType)
                RefreshHeldWeaponAttachment();
            WeaponPartsRefreshStruct refreshData = new WeaponPartsRefreshStruct();
            refreshData.weaponInfo = agent.ComponentScan;
            refreshData.slot = slot;
            refreshData.oldParts = lastParts;
            refreshData.newParts = agent.BaseComponent.CreateParts();
            RefreshModelWeaponParts(refreshData);
            return true;
        }

        /// <summary>
        /// API:parts
        /// </summary>
        /// <param name                          ="id"></param>
        /// <returns></returns>
        public bool SetWeaponPart(int id)
        {
            return SetWeaponPart(HeldSlotType, id);
        }

        /// <summary>
        /// API:parts
        /// </summary>
        /// <param name                          ="slot"></param>
        /// <param name                          ="partType"></param>
        public void DeleteWeaponPart(EWeaponSlotType slot, EWeaponPartType partType)
        {

            var agent = GetWeaponAgent(slot);
            if (!agent.IsValid()) return;
            WeaponPartsStruct lastParts = agent.BaseComponent.CreateParts();
            var parts = WeaponPartUtil.ModifyPartItem(
                agent.BaseComponent.CreateParts(), partType,
                UniversalConsts.InvalidIntId);
            agent.BaseComponent.ApplyParts(parts);
            if (slot == HeldSlotType)
                RefreshHeldWeaponAttachment();
            var newParts = WeaponPartUtil.ModifyPartItem(lastParts, partType, UniversalConsts.InvalidIntId);
            WeaponPartUtil.CombineDefaultParts(ref newParts,agent.BaseComponent.ConfigId);
            WeaponPartsRefreshStruct refreshData = new WeaponPartsRefreshStruct();
            refreshData.weaponInfo = agent.ComponentScan;
            refreshData.slot = slot;
            refreshData.oldParts = lastParts;
            refreshData.newParts = newParts;
            RefreshModelWeaponParts(refreshData);
        }
    }
}
