using Core;
using Core.EntityComponent;


namespace App.Shared
{

    /// <summary>
    /// Defines the <see cref="WeaponPartsRefreshStruct" />
    /// </summary>
    public struct WeaponPartsRefreshStruct
    {
        public WeaponScanStruct weaponInfo;

        public EWeaponSlotType slot;

        public WeaponPartsStruct oldParts;

        public WeaponPartsStruct newParts;

        public bool armInPackage;

        public EntityKey lastWeaponKey;

    
    }
    //public delegate void WeaponSlotUpdateEvent(EntityKey key);
//    public delegate void WeaponHeldUpdateEvent();
    public delegate void WeaponProcessEvent(IPlayerWeaponProcessor controller, EWeaponSlotType slot);
    public delegate void WeaponDropEvent(IPlayerWeaponProcessor controller, EWeaponSlotType slot, EntityKey dropedWeapon);
}
