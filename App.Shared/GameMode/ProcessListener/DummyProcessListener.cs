using Core;
using Core.EntityComponent;

namespace App.Shared.GameMode
{
    /// <summary>
    /// Defines the <see cref="DummyProcessListener" />
    /// </summary>
    public class DummyProcessListener : IWeaponProcessListener
    {
        public void OnExpend(IPlayerWeaponProcessor controller, EWeaponSlotType slot)
        {
        }

        public void OnPickup(IPlayerWeaponProcessor controller, EWeaponSlotType slot)
        {
        }

        public void OnDrop(IPlayerWeaponProcessor controller, EWeaponSlotType slot, EntityKey dropedWeapon)
        {
        }
    }
}
