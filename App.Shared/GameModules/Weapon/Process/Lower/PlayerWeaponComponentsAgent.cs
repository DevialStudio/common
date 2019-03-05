using App.Shared.Components.Player;
using App.Shared.Components.Weapon;
using Core;
using Core.EntityComponent;
using Core.Utils;
using System;

namespace App.Shared.GameModules.Weapon
{
    /// <summary>
    /// Defines the <see cref="PlayerWeaponComponentsAgent" />
    /// </summary>
    public class PlayerWeaponComponentsAgent
    {
        /// <summary>
        /// WeaponStateComponent
        /// </summary>
        private readonly Func<PlayerWeaponBagSetComponent>     playerWeaponBagExtractor;

        private readonly Func<PlayerWeaponUpdateComponent>           playerWeaponUpdateExtractor;

        private readonly Func<PlayerWeaponAuxiliaryComponent>  playerWeaponAuxiliaryExtractor;

        private readonly Func<PlayerWeaponCustomizeComponent>  playerCustomizeComponent;



        /// <summary>
        /// template cache，reduce gc
        /// </summary>



        public PlayerWeaponComponentsAgent(
                    Func<PlayerWeaponBagSetComponent> in_bagExtractor, Func<PlayerWeaponUpdateComponent> in_playerWeaponUpdateExtractor, Func<PlayerWeaponAuxiliaryComponent> in_playerWeaponAuxiliaryExtractor, Func<PlayerWeaponCustomizeComponent> in_playerCustomizeExtractor)
        {
            playerWeaponBagExtractor         = in_bagExtractor;
            playerWeaponUpdateExtractor      = in_playerWeaponUpdateExtractor;
            playerWeaponAuxiliaryExtractor   = in_playerWeaponAuxiliaryExtractor;
            playerCustomizeComponent         = in_playerCustomizeExtractor;

        }

        internal void RemoveBagWeapon(EWeaponSlotType slot,int bagIndex)
        {
            var slotData = BagSetCache[bagIndex][slot];
            slotData.Remove(EmptyWeaponKey);//player slot 数据移除
        }
        internal void ClearBagPointer()
        {
            BagSetCache.ClearPointer();
        }
        internal void AddBagWeapon(EWeaponSlotType slot, EntityKey key,int bagIndex)
        {
            BagSetCache.SetSlotWeaponData(bagIndex, slot, key);
        }
      
        internal void SetHeldSlotType(EWeaponSlotType slot)
        {
            BagSetCache.SetHeldSlotIndex(-1, (int)slot);
        }
        internal int BagLength { get { return BagSetCache.UsableLength; } }
       
        internal Func<EntityKey> GenerateWeaponKeyExtractor(EWeaponSlotType slotType, int bagIndex)
        {
            return () => { return BagSetCache[bagIndex][slotType].WeaponKey; };
        }
        internal Func<EntityKey> GenerateEmptyKeyExtractor()
        {
            return () => { return EmptyWeaponKey; };
        }


        /// <summary>
        /// 手雷物品自动填充
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="autoStuffSlot"></param>
        /// <returns></returns>
        internal WeaponBasicDataComponent TryStuffEmptyGrenadeSlot(EWeaponSlotType slot, bool autoStuffSlot = false)
        {
            return null;
        }

        /// <summary>
        /// 自动查找当前可用手雷,no vertify
        /// </summary>
        /// <param name="grenadeComp"></param>
        internal void TryStuffEmptyGrenadeSlot()
        {
        }

        private PlayerWeaponBagSetComponent BagSetCache
        {
            get{   return playerWeaponBagExtractor();}
        }
        internal PlayerWeaponUpdateComponent WeaponUpdateCache
        {
            get { return playerWeaponUpdateExtractor(); }
        }

    
        internal PlayerWeaponAuxiliaryComponent AuxCache
        {
            get { return playerWeaponAuxiliaryExtractor(); }
        }
        internal EntityKey EmptyWeaponKey { get { return playerCustomizeComponent().EmptyConstWeaponkey; } }
      //  internal EntityKey GrenadeWeaponKey { get { return playerCustomizeComponent().GrenadeConstWeaponKey; } }

        internal EWeaponSlotType HeldSlotType { get { return (EWeaponSlotType)BagSetCache.HeldSlotIndex; } }
     
        internal EWeaponSlotType LastSlotType { get { return   (EWeaponSlotType)BagSetCache.LastSlotIndex; } }

        public int HeldBagPointer
        {
            get { return BagSetCache.HeldBagPointer; }
            set { BagSetCache.HeldBagPointer = value; }
        }


     

  
       
    }
}
