using App.Shared.Audio;
using App.Shared.GameMode;
using App.Shared.Util;
using Assets.Utils.Configuration;
using Core;
using Core.Attack;
using Core.CharacterState;
using Core.Utils;
using System;
using System.Text;
using Utils.Appearance;
using Utils.Singleton;
using WeaponConfigNs;

namespace App.Shared.GameModules.Weapon
{
    /// <summary>
    /// Defines the <see cref="PlayerWeaponController" />
    /// </summary>
    public partial class PlayerWeaponController : IPlayerWeaponProcessor
    {
        public event WeaponDropEvent onWeaponDropEvt;

        public event WeaponProcessEvent onWeaponPickEvt;

        public event WeaponProcessEvent onWeaponExpendEvt;

        public event WeaponProcessEvent onWeaponSlotCleanupEvt;

        public void SetProcessListener(IWeaponProcessListener processListener)
        {
            onWeaponDropEvt += processListener.OnDrop;
            onWeaponExpendEvt += processListener.OnExpend;
            onWeaponPickEvt += processListener.OnPickup;
        }

        public void DrawWeapon(EWeaponSlotType slot, bool includeAction = true)
        {
            if (HeldSlotType == slot)
                return;
            WeaponScanStruct lastWeapon = HeldWeaponAgent.ComponentScan;
            if (!GetWeaponAgent(slot).IsValid()) return;
            var destWeapon = GetWeaponAgent(slot).ComponentScan;
            AppearanceSpecific();
            DoDrawInterrupt();
            bool armOnLeft = slot == EWeaponSlotType.SecondaryWeapon;
            float holsterParam = WeaponUtil.GetHolsterParam(HeldSlotType == EWeaponSlotType.SecondaryWeapon);
            float drawParam = armOnLeft ? AnimatorParametersHash.Instance.DrawLeftValue : AnimatorParametersHash.Instance.DrawRightValue;
            if (includeAction)
            {
                float switchParam = holsterParam * 10 + drawParam;
                if (lastWeapon.IsSafeVailed)
                {
                    RelatedStateInterface.SwitchWeapon(() => WeaponToHand(destWeapon.ConfigId, lastWeapon.ConfigId, slot, armOnLeft),
                        () => OnDrawWeaponCallback(destWeapon, slot), switchParam);
                }
                else
                {
                    WeaponToHand(destWeapon.ConfigId, lastWeapon.ConfigId, slot, armOnLeft);
                    OnSlotArmFinish(destWeapon, slot);
                    RelatedStateInterface.Draw(RelatedAppearence.RemountP3WeaponOnRightHand, drawParam);
                }
            }
            else
            {
                //CharacterState控制动作相关
                WeaponToHand(destWeapon.ConfigId, lastWeapon.ConfigId, slot, armOnLeft);
                OnDrawWeaponCallback(destWeapon, slot);
                OnSlotArmFinish(destWeapon, slot);
                RelatedAppearence.RemountP3WeaponOnRightHand();
            }
        }

        public void TryArmWeapon(EWeaponSlotType slot)
        {
            if (HeldSlotType != EWeaponSlotType.None)
                return;
            var agent = GetWeaponAgent(slot);
            if (!agent.IsValid()) return;
            // if (!currWeapon.IsSafeVailed) return;
            WeaponToHand(agent.ConfigId, HeldConfigId, slot);
            OnSlotArmFinish(agent.ComponentScan, slot);
        }

        public void UnArmHeldWeapon(Action onfinish)
        {
            WeaponScanStruct weaponInfo = HeldWeaponAgent.ComponentScan;
            AppearanceSpecific();
            float holsterParam = WeaponUtil.GetHolsterParam(HeldSlotType);
            RelatedStateInterface.CharacterUnmount(() => OnUnArmWeaponCallback(weaponInfo.ConfigId, onfinish), holsterParam);
        }

        public void UnArmHeldWeapon()//float holsterParam)
        {
            UnArmHeldWeapon(null);
        }

        public void ForceUnArmHeldWeapon()
        {
            RelatedAppearence.UnmountWeaponFromHand();
            SetHeldSlotTypeProcess(EWeaponSlotType.None);
            weaponInteract.ThrowActionExecute();
        }

        public bool DropWeapon(EWeaponSlotType slotType = EWeaponSlotType.Pointer)
        {
            if (slotType == EWeaponSlotType.Pointer) slotType = HeldSlotType;
            return DropWeapon(slotType, -1);
        }

        /// <summary>
        /// 主动扔把枪到地上，手雷不能丢
        /// </summary>
        /// <param name="slotType"></param>
        /// <param name="destroyImmediately"></param>
        /// <returns>是否创建场景掉落武器</returns>
        public bool DropWeapon(EWeaponSlotType slotType, int bagIndex)
        {
            if (!weaponProcessor.FilterDrop(slotType)) return false;
            return DestroyWeapon(slotType, bagIndex, true);
        }

        public bool DestroyWeapon(EWeaponSlotType slotType, int bagIndex, bool interrupt = true)
        {
            var weaponAgent = GetWeaponAgent(slotType, bagIndex);
            if (!weaponAgent.IsValid()) return false;
            if (WeaponUtil.IsC4p(weaponAgent.ConfigId))
                RelatedAppearence.RemoveC4();
            weaponAgent.ReleaseWeapon();
            //移除武器背包操作
            playerWeaponAgent.RemoveBagWeapon(slotType, bagIndex);
            if (IsHeldBagSlotType(slotType, bagIndex))
            {
                SetHeldSlotTypeProcess(EWeaponSlotType.None);
            }
            if (bagIndex == -1 || bagIndex == HeldBagPointer)
            {
                WeaponInPackage pos = slotType.ToWeaponInPackage();
                RelatedAppearence.UnmountWeaponInPackage(pos);
                if (interrupt)
                    Interrupt();
            }
            return true;
        }

        /// <summary>
        /// 自动拾取
        /// </summary>
        /// <param name="orient"></param>
        /// <returns>返回成功与否</returns>
        public bool AutoPickUpWeapon(WeaponScanStruct orient)
        {
            var slotType = weaponProcessor.GetMatchedSlot(orient.ConfigId);
            if (!weaponProcessor.FilterVailed(orient, slotType)) return false;
            if (!weaponProcessor.FilterAutoPickup(slotType)) return false;
            if (ReplaceWeaponToSlot(slotType, orient))
            {
                TryArmWeapon(slotType);
                if (onWeaponPickEvt != null)
                    onWeaponPickEvt(this, slotType);
            }
            return true;
        }

        /// <summary>
        /// pickup中的老物体会自动移出玩家身上
        /// </summary>
        /// <param name="orient"></param>
        /// <returns>返回是否执行生存掉落场景物体</returns>
        public bool PickUpWeapon(WeaponScanStruct orient, bool arm = true)
        {

            var slotType = weaponProcessor.GetMatchedSlot(orient.ConfigId);
            if (!weaponProcessor.FilterVailed(orient, slotType)) return false;
            if (!weaponProcessor.FilterPickup(slotType)) return false;
            if (ReplaceWeaponToSlot(slotType, orient))
            {
                if (arm)
                    TryArmWeapon(slotType);
                if (onWeaponPickEvt != null)
                    onWeaponPickEvt(this, slotType);
                return slotType != EWeaponSlotType.ThrowingWeapon;
            }
            return false;
        }

        public bool PickUpWeapon(WeaponScanStruct orient, ref bool pickupSuccess, bool arm = true)
        {

            var slotType = weaponProcessor.GetMatchedSlot(orient.ConfigId);
            if (!weaponProcessor.FilterVailed(orient, slotType)) return false;
            if (!weaponProcessor.FilterPickup(slotType)) return false;
            if (ReplaceWeaponToSlot(slotType, orient))
            {
                if (arm)
                    TryArmWeapon(slotType);
                if (onWeaponPickEvt != null)
                    onWeaponPickEvt(this, slotType);
                pickupSuccess = slotType != EWeaponSlotType.ThrowingWeapon;
                return pickupSuccess;
            }
            return false;
        }

        public void SwitchIn(EWeaponSlotType in_slot)
        {

            if (!weaponProcessor.FilterSwitchIn(in_slot))
            {
                weaponInteract.ShowTip(Core.Common.ETipType.NoWeaponInSlot);
                return;
            }

            if (IsHeldBagSlotType(in_slot))
            {
                SameSpeciesSwitchIn(in_slot);
            }
            else
            {
                DrawWeapon(in_slot);
                GameAudioMedia.PlayWeaponAudio(HeldWeaponAgent.ConfigId, RelatedAppearence.WeaponHandObject(), (item) => item.SwitchIn);
            }
        }

        public void PureSwitchIn(EWeaponSlotType in_slot)
        {
            if (in_slot == EWeaponSlotType.None)
                return;
            EWeaponSlotType from_slot = playerWeaponAgent.HeldSlotType;

            //int from_Id= componentAgent.GetSlotWeaponId(from_slot);

            if (IsWeaponSlotEmpty(in_slot))
            {
                weaponInteract.ShowTip(Core.Common.ETipType.NoWeaponInSlot);
                return;
            }
            if (!IsHeldBagSlotType(in_slot))
            {
                DrawWeapon(in_slot, false);
            }
        }

        public void ExpendAfterAttack()
        {
            if (HeldSlotType == EWeaponSlotType.None || IsHeldSlotEmpty)
                return;
            PlayFireAudio();
            //var handler = slotsAux.FindHandler(slot);
            bool destroyAndStuffNew = HeldWeaponAgent.ExpendWeapon();
            if (destroyAndStuffNew)
                AutoStuffHeldWeapon();
            if (onWeaponExpendEvt != null) onWeaponExpendEvt(this, HeldSlotType);
        }
        /// <summary>
        /// 切换背包操作
        /// </summary>
        /// <param name="pointer"></param>
        private void SwitchBag(int pointer)
        {
            if (pointer == HeldBagPointer) return;
            var lastHeldSlot = HeldSlotType;
            playerWeaponAgent.ClearBagPointer();
            ResetBagLockState();
            playerWeaponAgent.HeldBagPointer = pointer;
            DebugUtil.MyLog("switch Pointer:" + pointer,DebugUtil.DebugColor.Green);
            playerWeaponAgent.SetHeldSlotType(EWeaponSlotType.None);
            for (EWeaponSlotType slot = EWeaponSlotType.None + 1; slot < EWeaponSlotType.Length; slot++)
            {
                RefreshWeaponAppearance(slot);
            }
            if (GetWeaponAgent(lastHeldSlot).IsValid())
                TryArmWeapon(lastHeldSlot);
            else
                TryArmWeapon(PollGetLastSlotType());
            DebugUtil.MyLog("switch Pointer finished", DebugUtil.DebugColor.Green);
        }
        public void SwitchBag()
        {
            if (CanSwitchWeaponBag)
            {
                int length = ModeController.GetUsableWeapnBagLength(RelatedPlayerInfo);
                SwitchBag((HeldBagPointer + 1) % length);
            }
        }

        /// <summary>
        /// initialize bag state without arm
        /// </summary>
        /// <param name="pointer"></param>
        public void InitBag(int pointer)
        {
            playerWeaponAgent.ClearBagPointer();
            playerWeaponAgent.HeldBagPointer = pointer;
            ResetBagLockState();
        }

        /// <summary>
        ///通用填充武器逻辑
        /// </summary>
        /// <param name="slotType"></param>
        /// <param name="orient"></param>
        /// <returns></returns>
        public bool ReplaceWeaponToSlot(EWeaponSlotType slotType, WeaponScanStruct orient)
        {
            if (TryHoldGrenade(orient.ConfigId))
                return true;
            return ReplaceCommonWeapon(slotType, orient, HeldBagPointer);
        }

        /// <summary>
        /// 通用填充武器逻辑
        /// </summary>
        /// <param name="slotType"></param>
        /// <param name="bagIndex"></param>
        /// <param name="orient"></param>
        /// <returns></returns>
        public bool ReplaceWeaponToSlot(EWeaponSlotType slotType, int bagIndex, WeaponScanStruct orient)
        {
            if (TryHoldGrenade(orient.ConfigId))
                return true;
            return ReplaceCommonWeapon(slotType, orient, bagIndex);
        }

        /// <summary>
        /// 更新到槽位但不是拿在手上
        /// 老的武器entity会被重置或销毁掉
        /// </summary>
        /// <param name="slotType"></param>
        /// <param name="orient"></param>
        /// <param name="bagIndex"></param>
        /// <param name="useBagGlobal"></param>
        /// <returns></returns>
        private bool ReplaceCommonWeapon(EWeaponSlotType slotType, WeaponScanStruct orient, int bagIndex)
        {
            //  if (vertify)
            if (!weaponProcessor.FilterVailed(orient, slotType)) return false;
            bool refreshAppearance = (bagIndex == HeldBagPointer || bagIndex < 0);
            //特殊全局性武器只取武器背包第0个索引值
            var weaponAgent = GetWeaponAgent(slotType, bagIndex);
            WeaponPartsRefreshStruct refreshParams = new WeaponPartsRefreshStruct();
            WeaponEntity newEntity = weaponAgent.ReplaceWeapon(Owner, orient, ref refreshParams);
            if (newEntity == null) return false;
            playerWeaponAgent.AddBagWeapon(slotType, newEntity.entityKey.Value, bagIndex);
            if (refreshAppearance)
                RefreshModelWeaponParts(refreshParams);
            return true;
        }
        ///当前背包索引更改后的外观刷新操作
        private void RefreshWeaponAppearance(EWeaponSlotType slot =EWeaponSlotType.Pointer)
        {
            var weaponAgent = GetWeaponAgent(slot);
            if (weaponAgent.IsValid())
            {
                WeaponPartsRefreshStruct refreshParams = new WeaponPartsRefreshStruct();
                refreshParams.slot = slot;
                refreshParams.weaponInfo = weaponAgent.ComponentScan;
               // refreshParams.oldParts = new WeaponPartsStruct();
                refreshParams.newParts = weaponAgent.PartsScan;
                refreshParams.armInPackage = true;
                RefreshModelWeaponParts(refreshParams);
            }
            else
            {
                //不移除C4 RelatedAppearence.RemoveC4();
                WeaponInPackage pos = slot.ToWeaponInPackage();
                RelatedAppearence.UnmountWeaponInPackage(pos);
                Interrupt();

            }

        }

        //private bool SwitchGrenade(bool autoStuff = false)
        //{
        //    var weaponAgent = GetWeaponAgent(EWeaponSlotType.ThrowingWeapon, 0);
        //    int nextId = grenadeHelper.FindUsable(false);
        //    if (nextId == weaponAgent.ConfigId) return false;
        //    WeaponPartsRefreshStruct refreshParams = new WeaponPartsRefreshStruct();
        //    WeaponEntity newEntity = weaponAgent.ReplaceWeapon(Owner, WeaponUtil.CreateScan(nextId), ref refreshParams);
        //    if (newEntity == null) return false;
        //    playerWeaponAgent.AddBagWeapon(EWeaponSlotType.ThrowingWeapon, newEntity.entityKey.Value, 0);
        //    RefreshModelWeaponParts(refreshParams);
        //    return true;
        //}

        /// <summary>
        /// 添加手雷并尝试自动填充
        /// </summary>
        /// <param name="greandeId"></param>
        public bool TryHoldGrenade(int greandeId)
        {
            if (grenadeHelper.AddCache(greandeId))
            {
                TryHoldGrenade();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 自动装填手雷到腰间，手雷已装备情况下忽略
        /// </summary>
        public void TryHoldGrenade(bool autoStuff = true, bool tryArm = true)
        {
            var weaponAgent = GetWeaponAgent(EWeaponSlotType.ThrowingWeapon, 0);
            if (autoStuff && weaponAgent.IsValid()) return;
            int nextId = grenadeHelper.FindUsable(autoStuff);
            if (nextId < 0 || weaponAgent.ConfigId == nextId) return;
            WeaponPartsRefreshStruct refreshParams = new WeaponPartsRefreshStruct();
            WeaponEntity newEntity = weaponAgent.ReplaceWeapon(Owner, WeaponUtil.CreateScan(nextId), ref refreshParams);
            playerWeaponAgent.AddBagWeapon(EWeaponSlotType.ThrowingWeapon, newEntity.entityKey.Value, 0);
            RefreshModelWeaponParts(refreshParams);
            if(tryArm)
                TryArmWeapon(EWeaponSlotType.ThrowingWeapon);
        }

        public void RemoveGreande(int id)
        {
            int leftCount = grenadeHelper.RemoveCache(id);
            //其他行为通过回调来做
            if (leftCount < 0) return;
            var weaponAgent = GetWeaponAgent(EWeaponSlotType.ThrowingWeapon, 0);
            if (weaponAgent.ConfigId == id)
            {
                DestroyWeapon(EWeaponSlotType.ThrowingWeapon, 0);
                TryHoldGrenade();
            }
            RefreshWeaponAppearance();
        }

        public void Interrupt()
        {
            weaponInteract.CharacterInterrupt();
        }

        public void SetReservedBullet(int count)
        {
            var currSlot = HeldSlotType;
            if (currSlot.IsSlotWithBullet())
                ModeController.SetReservedBullet(this, HeldSlotType, count);
        }

        public void SetReservedBullet(EWeaponSlotType slot, int count)
        {
            if (slot.IsSlotWithBullet())
                ModeController.SetReservedBullet(this, slot, count);
        }

        public int SetReservedBullet(EBulletCaliber caliber, int count)
        {
            return ModeController.SetReservedBullet(this, caliber, count);
        }

        private void SameSpeciesSwitchIn(EWeaponSlotType slot)
        {
            if (!weaponProcessor.FilterSameSepeciesSwitchIn(slot)) return;
            int nextId = GetWeaponAgent(slot).FindNextWeapon(false);
            if (nextId > 0 && slot == EWeaponSlotType.ThrowingWeapon)
            {
                DebugUtil.MyLog("try switch next grenade:" + nextId,DebugUtil.DebugColor.Blue);
                TryHoldGrenade(false);
            }



        }

        public void CreateSetMeleeAttackInfo(MeleeAttackInfo attackInfo, MeleeFireLogicConfig config)
        {
            weaponInteract.CreateSetMeleeAttackInfo(attackInfo, config);
        }

        private void SetHeldSlotTypeProcess(EWeaponSlotType slotType)
        {
            playerWeaponAgent.SetHeldSlotType(slotType);
            RefreshHeldWeaponDetail();
        }

        private void OnDrawWeaponCallback(WeaponScanStruct weapon, EWeaponSlotType slot)
        {
            OnSlotArmFinish(weapon, slot);
            RelatedAppearence.RemountP3WeaponOnRightHand();
        }

        private void OnUnArmWeaponCallback(int weaponId, Action onfinish)
        {
            RelatedAppearence.UnmountWeaponFromHand();
            SetHeldSlotTypeProcess(EWeaponSlotType.None);
            if (WeaponUtil.IsC4p(weaponId))
            {
                weaponInteract.UnmountC4();
            }
            if (onfinish != null)
                onfinish();
        }

        private void RefreshHeldWeaponAttachment()
        {
            if (!weaponProcessor.FilterRefreshWeapon()) return;
            var baseComp = HeldWeaponAgent.ComponentScan;
            var attachments = baseComp.CreateParts();
            weaponInteract.ApperanceRefreshABreath(HeldWeaponAgent.BreathFactor);
            // 添加到背包的时候会执行刷新模型逻辑
            weaponInteract.ModelRefreshWeaponModel(baseComp.ConfigId, HeldSlotType, attachments);
        }

        private void RefreshModelWeaponParts(WeaponPartsRefreshStruct refreshData)
        {

            if (refreshData.armInPackage)
            {
                var avatarId = refreshData.weaponInfo.AvatarId;
                if (avatarId < 1)
                    avatarId = SingletonManager.Get<WeaponResourceConfigManager>().GetConfigById(refreshData.weaponInfo.ConfigId).AvatorId;
                if (WeaponUtil.IsC4p(refreshData.weaponInfo.ConfigId))
                {
                    OverrideBagTactic = refreshData.weaponInfo.ConfigId;
                    weaponInteract.UnmountC4();
                }
                else
                {
                    RelatedAppearence.MountWeaponInPackage(refreshData.slot.ToWeaponInPackage(), avatarId);
                }
            }
            weaponInteract.ModelRefreshWeaponParts(refreshData.weaponInfo.ConfigId, refreshData.slot, refreshData.oldParts, refreshData.newParts);
            if (refreshData.lastWeaponKey.IsValid())
            {
                if (refreshData.slot == HeldSlotType)
                    RefreshHeldWeapon();
                ////var handler = slotsAux.FindHandler(refreshData.slot);

                //if (refreshData.lastWeaponId != refreshData.weaponInfo.ConfigId)
                //    handler.RecordLastWeaponId(refreshData.lastWeaponId);
            }
        }

        private void RefreshHeldWeaponDetail()
        {
            RefreshHeldWeapon();
            // 需要执行刷新配件逻辑，因为配件会影响相机动作等属性
            RefreshHeldWeaponAttachment();
        }

        private void AppearanceSpecific()
        {
            if (playerWeaponAgent.HeldSlotType == EWeaponSlotType.SecondaryWeapon)
                RelatedAppearence.MountP3WeaponOnAlternativeLocator();
        }

        private void DoDrawInterrupt()
        {
            weaponInteract.CharacterDrawInterrupt();
        }

        private void WeaponToHand(int weaponId, int lastWeaponId, EWeaponSlotType slot, bool armOnLeft = false)
        {
            if (WeaponUtil.IsC4p(lastWeaponId))
            {
                weaponInteract.UnmountC4();
            }
            if (WeaponUtil.IsC4p(weaponId))
            {
                RelatedAppearence.MountC4(weaponId);
            }
            WeaponInPackage pos = slot.ToWeaponInPackage();
            RelatedAppearence.MountWeaponToHand(pos);
            if (armOnLeft)
                RelatedAppearence.MountP3WeaponOnAlternativeLocator();
        }

        private void OnSlotArmFinish(WeaponScanStruct weapon, EWeaponSlotType slot)
        {
            //TODO:
            SetHeldSlotTypeProcess(slot);
            if (weapon.Bullet <= 0)
            {
                if (SharedConfig.CurrentGameMode == EGameMode.Normal)
                {
                    //TODO 判断弹药数量是否足够，如果弹药不足，弹提示框
                    RelatedStateInterface.ReloadEmpty(() => { });
                }
            }
            else
            {
                //if (!bag.CurBolted)
                //{
                //    //TODO 拉栓动作
                //}
            }
        }

        private void AutoStuffHeldWeapon()
        {
            var lastSlotType = HeldSlotType;
            var nextId = HeldWeaponAgent.FindNextWeapon(true);
            //消耗掉当前武器
            DestroyWeapon(HeldSlotType, -1, false);
            //自动填充下一项武器
            if (lastSlotType == EWeaponSlotType.ThrowingWeapon)
                TryHoldGrenade();
            RefreshWeaponAppearance();
        }

        private void RefreshHeldWeapon()
        {
            RelatedOrient.Reset();

            if (IsHeldSlotEmpty)
                return;
        }
        //#if UNITY_EDITOR
        //        public WeaponBagDebugInfo Bag1DebugInfo = new WeaponBagDebugInfo();
        //        public WeaponBagDebugInfo Bag2DebugInfo = new WeaponBagDebugInfo();
        //        public WeaponBagDebugInfo Bag3DebugInfo = new WeaponBagDebugInfo();
        //        public WeaponBagDebugInfo Bag4DebugInfo = new WeaponBagDebugInfo();



        //        public void SyncDebugInfo()
        //        {
        //            //    int index = 0;
        //            //    Bag1DebugInfo.S0 = slotWeaponAgents[index, 0].WeaponKey;
        //            //    Bag1DebugInfo.S2 = slotWeaponAgents[index, 1].WeaponKey;
        //            //    Bag1DebugInfo.S3 = slotWeaponAgents[index, 2].WeaponKey;
        //            //    Bag1DebugInfo.S3 = slotWeaponAgents[index, 3].WeaponKey;
        //            //    Bag1DebugInfo.S4 = slotWeaponAgents[index, 4].WeaponKey;
        //            //    Bag1DebugInfo.S5 = slotWeaponAgents[index, 5].WeaponKey;
        //            //    Bag1DebugInfo.S6 = slotWeaponAgents[index, 6].WeaponKey;
        //            //    if (slotWeaponAgents.Length > 7)
        //            //    {
        //            //        index = 1;
        //            //        Bag2DebugInfo.S0 = slotWeaponAgents[index, 0].WeaponKey;
        //            //        Bag2DebugInfo.S1 = slotWeaponAgents[index, 1].WeaponKey;
        //            //        Bag2DebugInfo.S2 = slotWeaponAgents[index, 2].WeaponKey;
        //            //        Bag2DebugInfo.S3 = slotWeaponAgents[index, 3].WeaponKey;
        //            //        Bag2DebugInfo.S4 = slotWeaponAgents[index, 4].WeaponKey;
        //            //        Bag2DebugInfo.S5 = slotWeaponAgents[index, 5].WeaponKey;
        //            //        Bag2DebugInfo.S6 = slotWeaponAgents[index, 6].WeaponKey;
        //            //    }
        //            //    if (slotWeaponAgents.Length > 14)
        //            //    {
        //            //        index = 2;
        //            //        Bag3DebugInfo.S0 = slotWeaponAgents[index, 0].WeaponKey;
        //            //        Bag3DebugInfo.S1 = slotWeaponAgents[index, 1].WeaponKey;
        //            //        Bag3DebugInfo.S2 = slotWeaponAgents[index, 2].WeaponKey;
        //            //        Bag3DebugInfo.S3 = slotWeaponAgents[index, 3].WeaponKey;
        //            //        Bag3DebugInfo.S4 = slotWeaponAgents[index, 4].WeaponKey;
        //            //        Bag3DebugInfo.S5 = slotWeaponAgents[index, 5].WeaponKey;
        //            //        Bag3DebugInfo.S6 = slotWeaponAgents[index, 6].WeaponKey;
        //            //    }
        //            //    if (slotWeaponAgents.Length > 21)
        //            //    {
        //            //        index = 3;
        //            //        Bag4DebugInfo.S0 = slotWeaponAgents[index, 0].WeaponKey;
        //            //        Bag4DebugInfo.S1 = slotWeaponAgents[index, 1].WeaponKey;
        //            //        Bag4DebugInfo.S2 = slotWeaponAgents[index, 2].WeaponKey;
        //            //        Bag4DebugInfo.S3 = slotWeaponAgents[index, 3].WeaponKey;
        //            //        Bag4DebugInfo.S4 = slotWeaponAgents[index, 4].WeaponKey;
        //            //        Bag4DebugInfo.S5 = slotWeaponAgents[index, 5].WeaponKey;
        //            //        Bag4DebugInfo.S6 = slotWeaponAgents[index, 6].WeaponKey;
        //            //    }


        //            //}
        //#endif
    }
}
