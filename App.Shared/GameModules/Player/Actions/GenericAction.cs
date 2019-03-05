using App.Shared.Components.GenericActions;
using UnityEngine;

namespace App.Shared.GameModules.Player.Actions
{
    public class GenericAction : IGenericAction
    {
        private readonly IAction _climbAction = new ClimbUp(); //攀爬
        private readonly IAction _stepAction = new StepUp(); //台阶
        private readonly IAction _vaultAction = new Vault(); //翻越
        private IAction _concretenessAction;

        public void PlayerReborn(PlayerEntity player)
        {
            if (player.hasThirdPersonAnimator)
                player.thirdPersonAnimator.UnityAnimator.applyRootMotion = false;
            if (player.hasThirdPersonModel)
                player.thirdPersonModel.Value.transform.localPosition.Set(0, 0, 0);
            ResetConcretenessAction();
        }

        public void PlayerDead(PlayerEntity player)
        {
            if (player.hasThirdPersonAnimator)
                player.thirdPersonAnimator.UnityAnimator.applyRootMotion = false;
            if (player.hasThirdPersonModel)
                player.thirdPersonModel.Value.transform.localPosition.Set(0, 0, 0);
            ResetConcretenessAction();
        }

        public void Update(PlayerEntity player)
        {
            if (null != _concretenessAction)
            {
                _concretenessAction.Update();
                _concretenessAction.AnimationBehaviour();
            }
        }

        public void ActionInput(PlayerEntity player)
        {
            TestTrigger(player);
            if (null != _concretenessAction)
            {
                _concretenessAction.ActionInput(player);
            }
        }

        /**
         * 1.人物正前方做CapsuleCast(capsuleBottom向上微抬)
         * 2.hit点向上抬 探出碰撞体高 + 人物高  的距离
         * 3.向下做SphereCast(半径0.3)，目的是人物所站位置与攀爬位置有一定的容错
         * 4.hit点作为攀爬点，做MatchTarget(手到hit点差值)
         * 5.人物站立位置往正前方移动1m，做OverlapCapsule，检测翻越
         */
        private void TestTrigger(PlayerEntity player)
        {
            if (null == player) return;
            
            if (null != _concretenessAction && _concretenessAction.PlayingAnimation ||
                !ClimbUpCollisionTest.ClimbUpFrontDistanceTest(player))
            {
                ResetConcretenessAction();
                return;
            }

            GenericActionKind kind;
            Vector3 matchTarget;
            ClimbUpCollisionTest.ClimbUpTypeTest(player, out kind, out matchTarget);
            CreateConcretenessAction(kind, matchTarget);
        }

        private void CreateConcretenessAction(GenericActionKind kind, Vector3 matchTarget)
        {
            ResetConcretenessAction();
            
            switch (kind)
            {
                case GenericActionKind.Climb:
                    _concretenessAction = _climbAction;
                    break;
                case GenericActionKind.Step:
                    _concretenessAction = _stepAction;
                    break;
                case GenericActionKind.Vault:
                    _concretenessAction = _vaultAction;
                    break;
                case GenericActionKind.Null:
                    _concretenessAction = null;
                    break;
                default:
                    _concretenessAction = null;
                    break;
            }

            if (null == _concretenessAction) return;
            _concretenessAction.MatchTarget = matchTarget;
            _concretenessAction.CanTriggerAction = true;
        }

        private void ResetConcretenessAction()
        {
            if (null != _concretenessAction)
                _concretenessAction.ResetConcretAction();
            _concretenessAction = null;
        }
    }

    public enum GenericActionKind
    {
        Vault,
        Step,
        Climb,
        Null
    }
}