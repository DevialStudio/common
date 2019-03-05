using Assets.Utils.Configuration;
using Core.Free;
using Core.Utils;
using Core.WeaponLogic;
using Utils.Singleton;

namespace App.Shared.GameModules.Weapon.Behavior
{
    /// <summary>
    /// Defines the <see cref="WeaponLogicManager" />
    /// </summary>
    public class WeaponLogicManager : IWeaponLogicManager
    {
        private static readonly LoggerAdapter Logger = new LoggerAdapter(typeof(WeaponLogicManager));

        private IFireLogicProvider _fireLogicCreator;

        //private IWeaponDataConfigManager _weaponDataConfigManager;

        //private IWeaponResourceConfigManager  SingletonManager.Get<WeaponResourceConfigManager>();
        private IFreeArgs _freeArgs;

        private DefaultWeaponLogic _defaultWeaponLogic;

        public WeaponLogicManager(//WeaponConfigManagement weaponDataConfigManager,
                                  //IWeaponResourceConfigManager weaponConfigManager,
            IFireLogicProvider fireLogicCreator,
            IFreeArgs freeArgs)
        {
            _fireLogicCreator = fireLogicCreator;
            //_weaponDataConfigManager = weaponDataConfigManager;
            // SingletonManager.Get<WeaponResourceConfigManager>() = weaponConfigManager;
            _defaultWeaponLogic = new DefaultWeaponLogic();
            _freeArgs = freeArgs;
        }

        public IWeaponLogic GetWeaponLogic(int? weaponId)
        {
            var realWeaponId = weaponId;
            if (!weaponId.HasValue)
            {
                realWeaponId = WeaponUtil.EmptyHandId;
            }

            var weaponAllConfig = SingletonManager.Get<WeaponConfigManagement>().FindConfigById(realWeaponId.Value);
            if (weaponAllConfig.S_DefualtBehavior != null)
            {
                _defaultWeaponLogic.SetFireLogic(_fireLogicCreator.GetFireLogic(weaponAllConfig.NewWeaponCfg, weaponAllConfig.InitAbstractLogicConfig));
                return _defaultWeaponLogic;

            }
            else if (weaponAllConfig.S_TacticBehvior != null)
            {
                return new TacticWeaponLogic(realWeaponId.Value, _freeArgs);
            }
            else if (weaponAllConfig.S_DoubleBehavior != null)
            {
                return new DoubleWeaponLogic(_fireLogicCreator.GetFireLogic(weaponAllConfig.NewWeaponCfg, weaponAllConfig.S_DoubleBehavior.LeftFireLogic),
              _fireLogicCreator.GetFireLogic(weaponAllConfig.NewWeaponCfg, weaponAllConfig.S_DoubleBehavior.RightFireLogic));
            }
            return null;
        }

        public void ClearCache()
        {
            _fireLogicCreator.ClearCache();
        }
    }
}
