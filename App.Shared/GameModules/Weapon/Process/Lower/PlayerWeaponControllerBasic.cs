using App.Server.GameModules.GamePlay.free.player;
using App.Shared.Audio;
using App.Shared.Components.Player;
using App.Shared.Util;
using Core;
using Core.Appearance;
using Core.CharacterBone;
using Core.CharacterState;
using Core.Common;
using Core.EntityComponent;
using Core.Statistics;
using Core.WeaponLogic.Throwing;
using System;
using System.Collections.Generic;
using Utils.Configuration;
using Utils.Singleton;
using Utils.Utils;
using WeaponConfigNs;
using XmlConfig;

///     #region//service api
//partial void DrawWeapon(EWeaponSlotType slot, bool includeAction = true);
//public partial void TryArmWeapon(EWeaponSlotType slot);
//public partial void UnArmHeldWeapon(Action onfinish);
//public partial void ForceUnArmHeldWeapon();
//public partial void DropWeapon(EWeaponSlotType slot);
//public partial void RemoveWeapon(EWeaponSlotType slot, bool interrupt = true);
//public partial bool AutoPickUpWeapon(WeaponScanStruct orient);
//public partial EntityKey PickUpWeapon(WeaponScanStruct orient);
//public partial void SwitchIn(EWeaponSlotType in_slot);
//public partial void PureSwitchIn(EWeaponSlotType in_slot);
//public partial void ExpendAfterAttack(EWeaponSlotType slot);
//public partial bool ReplaceWeaponToSlot(EWeaponSlotType slotType, WeaponScanStruct orient);
//public partial bool ReplaceWeaponToSlot(EWeaponSlotType slotType, WeaponScanStruct orient, bool vertify, out EntityKey lastKey);
//public partial void Interrupt();
//public partial void SetReservedBullet(int count);
//public partial void SetReservedBullet(EWeaponSlotType slot, int count);
//public partial int SetReservedBullet(EBulletCaliber caliber, int count);
//public partial bool SetWeaponPart(EWeaponSlotType slot, int id);
//public partial void DeleteWeaponPart(EWeaponSlotType slot, EWeaponPartType part);

/// </summary>
namespace App.Shared.GameModules.Weapon
{
    /// <summary>
    /// Defines the <see cref="PlayerWeaponController" />
    /// </summary>
    public partial class PlayerWeaponController : ModuleLogicActivator<PlayerWeaponController>, IPlayerWeaponSharedGetter
    {
        private PlayerEntityWeaponInteract weaponInteract;

        public GameModeControllerBase ModeController
        {
            get { return GameModuleManagement.Get<GameModeControllerBase>(Owner.EntityId); }
        }

        //  private readonly WeaponSlotsAux slotsAux;
        private PlayerWeaponComponentsAgent playerWeaponAgent;

        private readonly WeaponProcessor weaponProcessor;

        public IGrenadeCacheHelper GrenadeHelper
        {
            get { return grenadeHelper; }
        }

        private GrenadeCacheHelper grenadeHelper;

        private WeaponBaseAgent[,] slotWeaponAgents;

        public bool CanUseGrenade
        {
            get { return weaponInteract.CanUseGreande(); }
        }

        private readonly Dictionary<EWeaponSlotType, System.Type> weaponAgentAssTypeDict = new Dictionary<EWeaponSlotType, Type>();

        /// <summary>
        /// 槽位武器监听事件
        /// </summary>
        private WeaponBaseAgent CreateGetWeaponAgent(int bagIndex, EWeaponSlotType slotType)
        {
            if (slotWeaponAgents[bagIndex, (int)slotType] == null)
            {
                var func1 = playerWeaponAgent.GenerateWeaponKeyExtractor(slotType, bagIndex);
                var func2 = playerWeaponAgent.GenerateEmptyKeyExtractor();
                var newAgent = (WeaponBaseAgent)Activator.CreateInstance(weaponAgentAssTypeDict[(EWeaponSlotType)slotType],
                    func1, func2, slotType, grenadeHelper);
                slotWeaponAgents[bagIndex, (int)slotType] = newAgent;

            }
            return slotWeaponAgents[bagIndex, (int)slotType];
        }

        //public override string ToString()
        //{
        //    //string s = "";
        //    //foreach(WeaponBagContainer val in playerWeaponAgent.BagSetCache.WeaponBags)
        //    //{
        //    //    s += val.ToString();
        //    //}
        //    ////return s;
        //}
        public PlayerWeaponController()
        {
            weaponProcessor = new WeaponProcessor(this);

            CommonUtil.ProcessDerivedTypes(typeof(WeaponBaseAgent), true, (Type t) => OnDerivedTypeInstanceProcess(t));
        }

        private void OnDerivedTypeInstanceProcess(System.Type t)
        {
            var attributes = Attribute.GetCustomAttributes(t, false);
            WeaponSpeciesAttribute speciesAttr;
            foreach (Attribute attr in attributes)
            {
                speciesAttr = attr as WeaponSpeciesAttribute;
                weaponAgentAssTypeDict.Add(speciesAttr.slotType, t);
            }
        }


        public void Initialize(EntityKey owner, PlayerWeaponComponentsAgent agent, PlayerEntityWeaponInteract interact, GrenadeCacheHelper helper)
        {
            Owner                = owner;
            grenadeHelper        = helper;
            weaponInteract       = interact;
            playerWeaponAgent    = agent;
            int modeBagLength    = ModeController.GetUsableWeapnBagLength(RelatedPlayerInfo);
            slotWeaponAgents     = new WeaponBaseAgent[GlobalConst.WeaponBagMaxCount, GlobalConst.WeaponSlotMaxLength];
            var throwWeaponAgent = CreateGetWeaponAgent(0, EWeaponSlotType.ThrowingWeapon);
            //多个背包共享一份投掷武器代理
            if (modeBagLength > 1)
            {
                for (int i = 1; i < modeBagLength; i++)
                    slotWeaponAgents[i, (int)EWeaponSlotType.ThrowingWeapon] = throwWeaponAgent;
            }

        }

        public void ResetAllComponents()
        {
            if (RelatedOrient != null)
                RelatedOrient.Reset();
        }

        public void ResetBagLockState()
        {
            BagLockState = false;
            BagOpenLimitTIme = RelatedTime.ClientTime + SingletonManager.Get<GameModeConfigManager>().GetBagLimitTime(ModeController.ModeId);
        }

        public void AddAuxBullet(PlayerBulletData bulletData)
        {
            if (playerWeaponAgent.AuxCache.BulletList != null)
                playerWeaponAgent.AuxCache.BulletList.Add(bulletData);
        }

        public void AddAuxEffect()
        {
            playerWeaponAgent.AuxCache.ClientInitialize = true;
            playerWeaponAgent.AuxCache.EffectList = new List<EClientEffectType>();
        }

        public void AddAuxBullet()
        {
            playerWeaponAgent.AuxCache.BulletList = new List<PlayerBulletData>();
        }

        public void AddAuxEffect(EClientEffectType effectType)
        {
            if (playerWeaponAgent.AuxCache.EffectList != null)
                playerWeaponAgent.AuxCache.EffectList.Add(effectType);
        }

        public EntityKey Owner { get; private set; }

        public EntityKey EmptyWeaponKey
        {
            get { return playerWeaponAgent.EmptyWeaponKey; }
        }

        public WeaponBaseAgent HeldWeaponAgent
        {
            get { return CreateGetWeaponAgent(HeldBagPointer, HeldSlotType); }
        }

        public int HeldConfigId
        {
            get { return HeldWeaponAgent.ConfigId; }
        }

        public WeaponBaseAgent GetWeaponAgent(EWeaponSlotType slotType = EWeaponSlotType.Pointer, int bagIndex = -1)
        {
            if (bagIndex < 0) bagIndex = HeldBagPointer;

            if (slotType == EWeaponSlotType.Pointer) slotType = HeldSlotType;
            else if (slotType == EWeaponSlotType.LastPointer) slotType = LastSlotType;
            return CreateGetWeaponAgent(bagIndex, slotType);
        }
        public WeaponBaseAgent GetWeaponAgent(int configId)
        {
            EWeaponSlotType slotType = weaponProcessor.GetMatchedSlot(configId);
            return CreateGetWeaponAgent(-1, slotType);
          
        }

        public bool IsWeaponInSlot(int configId, int bagIndex = -1)
        {
            EWeaponSlotType slotType = WeaponUtil.GetEWeaponSlotTypeById(configId);
            if (slotType == EWeaponSlotType.None) return false;
            if (bagIndex < 0) bagIndex = HeldBagPointer;
            return CreateGetWeaponAgent(bagIndex, slotType).ConfigId == configId;
        }


        public EWeaponSlotType HeldSlotType
        {
            get { return playerWeaponAgent.HeldSlotType; }
        }

        public bool IsHeldSlotEmpty
        {
            get { return !HeldWeaponAgent.IsValid(); }
        }

        public EWeaponSlotType LastSlotType
        {
            get
            {
                return playerWeaponAgent.LastSlotType;
            }
        }

        public int HeldBagPointer
        {
            get { return playerWeaponAgent.HeldBagPointer; }
        }

        public int GetReservedBullet(EBulletCaliber caliber)
        {
            return ModeController.GetReservedBullet(this, caliber);
        }

        public int GetReservedBullet()
        {
            return ModeController.GetReservedBullet(this, HeldSlotType);
        }

        public int GetReservedBullet(EWeaponSlotType slot)
        {
            if (slot.IsSlotWithBullet())
                return ModeController.GetReservedBullet(this, slot);
            return 0;
        }

        public bool IsWeaponSlotEmpty(EWeaponSlotType slot)
        {
            return !GetWeaponAgent(slot).IsValid();
            // return playerWeaponAgent.IsWeaponSlotEmpty(slot);
        }

        public bool IsHeldBagSlotType(EWeaponSlotType slot, int bagIndex = -1)
        {
            return (bagIndex < 0 || HeldBagPointer == bagIndex) && slot == HeldSlotType;
            //return playerWeaponAgent.IsHeldSlotType(slot, bagIndex);
        }

        public EWeaponSlotType PollGetLastSlotType(bool excludeLast = true)
        {
            if (!excludeLast)
            {
                EWeaponSlotType last = LastSlotType;
                if (last != EWeaponSlotType.None && !IsWeaponSlotEmpty(last))
                {
                    return last;
                }
            }
            for (EWeaponSlotType s = EWeaponSlotType.None + 1; s < EWeaponSlotType.Length; s++)
            {
                if (!IsWeaponSlotEmpty(s))
                    return s;
            }
            return EWeaponSlotType.None;

        }
        public void LateUpdate()
        {
            if (!HeldWeaponAgent.IsValid() && HeldSlotType != EWeaponSlotType.None)
                SetHeldSlotTypeProcess(EWeaponSlotType.None);
            if (playerWeaponAgent.WeaponUpdateCache.UpdateHeldAppearance)
            {
                playerWeaponAgent.WeaponUpdateCache.UpdateHeldAppearance = false;
                //率先刷新手雷物品
                TryHoldGrenade(true,false);
                RefreshWeaponAppearance(EWeaponSlotType.ThrowingWeapon);
                EWeaponSlotType newSlot = PollGetLastSlotType();
                if (newSlot == HeldSlotType)
                    RefreshWeaponAppearance();
                else
                    TryArmWeapon(newSlot);
            }

        }

        public List<PlayerBulletData> BulletList
        {
            get { return playerWeaponAgent.AuxCache.BulletList; }
        }

        public List<EClientEffectType> EffectList
        {
            get { return playerWeaponAgent.AuxCache.EffectList; }
        }

        public int ForceInterruptGunSight
        {
            get
            {
                if (playerWeaponAgent.AuxCache.ClientInitialize)
                    return playerWeaponAgent.AuxCache.ForceInterruptGunSight;
                return -1;
            }
            set { playerWeaponAgent.AuxCache.ForceInterruptGunSight = value; }
        }

        public int? AutoFire
        {
            get
            {

                if (playerWeaponAgent.AuxCache.HasAutoAction)
                    return playerWeaponAgent.AuxCache.AutoFire;
                return null;
            }
            set { playerWeaponAgent.AuxCache.AutoFire = value.Value; }
        }
        public int BagLength { get { return playerWeaponAgent.BagLength; } }
        public int BagOpenLimitTIme
        {
            get { return playerWeaponAgent.AuxCache.BagOpenLimitTime; }
            set { playerWeaponAgent.AuxCache.BagOpenLimitTime = value; }
        }

        public bool? AutoThrowing
        {
            get
            {

                if (playerWeaponAgent.AuxCache.HasAutoAction)
                    return playerWeaponAgent.AuxCache.AutoThrowing;
                return null;
            }
            set { playerWeaponAgent.AuxCache.AutoThrowing = value.Value; }
        }

        ///overridebag components
        public int OverrideBagTactic
        {
            get { return playerWeaponAgent.WeaponUpdateCache.TacticWeapon; }
            set { playerWeaponAgent.WeaponUpdateCache.TacticWeapon= value; }
        }

        public bool BagLockState
        {
            get { return playerWeaponAgent.AuxCache.BagLockState; }
            set { playerWeaponAgent.AuxCache.BagLockState = value; }
        }

        public bool CanSwitchWeaponBag
        {
            get { return ModeController.CanModeSwitchBag && !BagLockState && BagOpenLimitTIme < RelatedTime.ClientTime; }
        }

        public void PlayFireAudio()
        {
            if (!IsHeldSlotEmpty)
                GameAudioMedia.PlayWeaponAudio(HeldConfigId, RelatedAppearence.WeaponHandObject(), (config) => config.Fire);
        }
        public void PlayPullBoltAudio()
        {
            if (!IsHeldSlotEmpty)
                GameAudioMedia.PlayWeaponAudio(HeldConfigId, RelatedAppearence.WeaponHandObject(), (config) => config.PullBolt);
        }
        public OrientationComponent RelatedOrient
        {
            get { return weaponInteract.RelatedOrient; }
        }

        public FirePosition RelatedFirePos
        {
            get { return weaponInteract.RelatedFirePos; }
        }

        public TimeComponent RelatedTime
        {
            get { return weaponInteract.RelatedTime; }
        }

        public CameraFinalOutputNewComponent RelatedCameraFinal
        {
            get { return weaponInteract.RelatedCameraFinal; }
        }

        public ThrowingActionInfo RelatedThrowActionInfo
        {
            get { return weaponInteract.RelatedThrowAction.ActionInfo; }
        }

        public ICharacterState RelatedStateInterface
        {
            get { return weaponInteract.RelatedCharState.State; }
        }

        public ThrowingUpdateComponent RelatedThrowUpdate
        {

            get { return weaponInteract.RelatedThrowUpdate; }
        }

        public StatisticsData RelatedStatics
        {
            get { return weaponInteract.RelatedStatistics.Statistics; }
        }

        public CameraStateNewComponent RelatedCameraSNew
        {
            get { return weaponInteract.RelatedCameraSNew; }
        }

        public ICharacterAppearance RelatedAppearence
        {
            get { return weaponInteract.RelatedAppearence.Appearance; }
        }

        public ICharacterBone RelatedBones
        {
            get { return weaponInteract.RelatedBones.CharacterBone; }
        }

        public PlayerInfoComponent RelatedPlayerInfo
        {
            get { return weaponInteract.RelatedPlayerInfo; }
        }

        public PlayerMoveComponent RelatedPlayerMove
        {
            get { return weaponInteract.RelatedPlayerMove; }
        }

        public FreeData RelatedFreeData
        {
            get { return (FreeData)weaponInteract.RelatedFreeData.FreeData; }
        }

        public LocalEventsComponent RelatedLocalEvents
        {
            get { return weaponInteract.RelatedLocalEvents; }
        }

        public PlayerWeaponAmmunitionComponent RelatedAmmunition
        {
            get { return weaponInteract.RelatedAmmunition; }
        }

        public void ShowTip(ETipType tip)
        {
            weaponInteract.ShowTip(tip);
        }

        public void CreateSetMeleeAttackInfoSync(int atk)
        {
            weaponInteract.CreateSetMeleeAttackInfoSync(atk);
        }
    }
}
