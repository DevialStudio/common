using Core;
using Core.EntityComponent;
using Core.Utils;

namespace App.Shared.GameMode
{
    /// <summary>
    /// Defines the <see cref="NormalProcessListener" />
    /// </summary>
    public class NormalProcessListener : IWeaponProcessListener
    {
        private static readonly LoggerAdapter Logger = new LoggerAdapter(typeof(NormalProcessListener));

        public void OnExpend(IPlayerWeaponProcessor controller, EWeaponSlotType slot)
        {
            //TODO:音频播放
            //GameAudioMedium.PlayWeaponAudio(controller., RelatedAppearence.WeaponHandObject(), (item) => item.Fire);

            if (!slot.IsSlotChangeByCost())
            {
                return;
            }
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("{0} OnExpend", controller.Owner);
            }
            controller.BagLockState = true;
        }

        public void OnDrop(IPlayerWeaponProcessor controller, EWeaponSlotType slot, EntityKey key)
        {
            Logger.DebugFormat("{0} OnDrop", controller.Owner);
            controller.BagLockState = true;
        }

        public void OnPickup(IPlayerWeaponProcessor controller, EWeaponSlotType slot)
        {
            Logger.DebugFormat("{0} OnPickup", controller.Owner);
            controller.BagLockState = true;
        }

        private void LockPlayerBag(IPlayerWeaponProcessor controller)
        {
            Logger.DebugFormat("{0} LockPlayerBag", controller.Owner);
            controller.BagLockState = true;
        }
    }
}
