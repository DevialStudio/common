using App.Shared.Player;
using Core.Fsm;
using Core.Utils;
using UnityEngine;
using Utils.Appearance;
using Utils.Utils;

namespace App.Shared.GameModules.Player.Actions
{
    public static class ClimbUpCollisionTest
    {
        private static RaycastHit _hit;
        private static Vector3 _overlapPos = Vector3.zero;
        private static Vector3 _capsuleBottom = Vector3.zero;
        private static Vector3 _capsuleUp = Vector3.zero;
        private static float _capsuleRadius;
        private static float _capsuleHeight;
        
        private const float ProbeDist = 1.1f;

        // 探测前方1m是否有障碍物
        public static bool ClimbUpFrontDistanceTest(PlayerEntity player)
        {
            if(null == player) return false;
            CreateData(player);
            var playerTransform = player.RootGo().transform;
            return Physics.CapsuleCast(_capsuleBottom, _capsuleUp, 0.001f, playerTransform.forward, out _hit, 1,
                UnityLayers.ClimbCollisionLayerMask) && _hit.distance < 0.5;
        }
        
        public static void ClimbUpTypeTest(PlayerEntity player, out GenericActionKind climbUpKind, out Vector3 matchTarget)
        {
            DownRayTest(player, out climbUpKind, out matchTarget);
            if (GenericActionKind.Null == climbUpKind)
                AllRoundRayTest(player, out climbUpKind, out matchTarget);
        }

        // 自上而下
        private static void DownRayTest(PlayerEntity player, out GenericActionKind kind, out Vector3 matchTarget)
        {
            var sphereCenter = new Vector3(_hit.point.x,
                _hit.collider.bounds.center.y + _hit.collider.bounds.extents.y + _capsuleHeight, _hit.point.z);
            
            if (SourceInCollisionTest(sphereCenter, out kind, out matchTarget)) return;

            RaycastHit sphereHit;
            Physics.SphereCast(sphereCenter, 0.3f, Vector3.down, out sphereHit,
                _hit.collider.bounds.center.y + _hit.collider.bounds.extents.y + _capsuleHeight,
                UnityLayers.SceneCollidableLayerMask);

            if(VaultUpTest(sphereHit.point, player, out kind, out matchTarget)) return;
            if(ClimbUpTest(sphereHit.point, player, out kind, out matchTarget)) return;
            StepUpTest(sphereHit.point, player, out kind, out matchTarget);
        }

        private static bool ClimbUpTest(Vector3 collisionPoint, PlayerEntity player, out GenericActionKind kind, out Vector3 matchTarget)
        {
            var playerTransform = player.RootGo().transform;
            var distance = collisionPoint.y - playerTransform.position.y;
            if (distance > 1.5 && distance < 2.3)
            {
                kind = GenericActionKind.Climb;
                matchTarget = collisionPoint - playerTransform.right * 0.2f;
                return true;
            }
            
            kind = GenericActionKind.Null;
            matchTarget = Vector3.zero;
            return false;
        }
        
        // Vault测试需在Step测试之前
        private static bool VaultUpTest(Vector3 collisionPoint, PlayerEntity player, out GenericActionKind kind, out Vector3 matchTarget)
        {
            kind = GenericActionKind.Null;
            matchTarget = Vector3.zero;
            
            var playerTransform = player.RootGo().transform;
            var distance = collisionPoint.y - playerTransform.position.y;
            if (distance < 0.8 || distance > 2.3) return false;
            
            // 检测翻越过程中障碍
            _capsuleBottom.y = (collisionPoint + playerTransform.up * 0.3f).y;
            _capsuleUp.y = _capsuleBottom.y + _capsuleHeight;
            if (Physics.CapsuleCast(_capsuleBottom, _capsuleUp, 0.1f, playerTransform.forward,
                1, UnityLayers.SceneCollidableLayerMask)) return false;
            
            // 人物当前位置，往前移动1m，往上移动0.2m
            _overlapPos = playerTransform.position + playerTransform.forward * (1.0f + _capsuleRadius) +
                          playerTransform.up * 0.2f;
            PlayerEntityUtility.GetCapsule(player, _overlapPos, out _capsuleBottom, out _capsuleUp,
                out _capsuleRadius);
            if (Physics.OverlapCapsule(_capsuleBottom, _capsuleUp, _capsuleRadius,
                    UnityLayers.SceneCollidableLayerMask).Length > 0) return false;

            kind = GenericActionKind.Vault;
            matchTarget = collisionPoint - playerTransform.right * 0.2f;
            return true;
        }

        private static void StepUpTest(Vector3 collisionPoint, PlayerEntity player, out GenericActionKind kind, out Vector3 matchTarget)
        {
            var playerTransform = player.RootGo().transform;
            var distance = collisionPoint.y - playerTransform.position.y;
            if(distance > 0.5 && distance <= 1.5)
            {
                kind = GenericActionKind.Step;
                matchTarget = collisionPoint - playerTransform.right * 0.2f;
                return;
            }
            
            kind = GenericActionKind.Null;
            matchTarget = Vector3.zero;
        }

        //四方向
        private static void AllRoundRayTest(PlayerEntity player, out GenericActionKind kind, out Vector3 matchTarget)
        {
            kind = GenericActionKind.Null;
            matchTarget = Vector3.zero;
        }

        private static bool SourceInCollisionTest(Vector3 sourcePoint, out GenericActionKind kind, out Vector3 matchTarget)
        {
            kind = GenericActionKind.Null;
            matchTarget = Vector3.zero;
            return Physics.OverlapSphere(sourcePoint, 0.3f, UnityLayers.SceneCollidableLayerMask).Length > 0;
        }
        
        private static void CreateData(PlayerEntity player)
        {
            if(null == player) return;
            var playerTransform = player.RootGo().transform;
            _overlapPos = playerTransform.position;
            
            PlayerEntityUtility.GetCapsule(player, _overlapPos, out _capsuleBottom,
                out _capsuleUp, out _capsuleRadius);
            _capsuleHeight = _capsuleUp.y - _capsuleBottom.y;
            _capsuleBottom.y = (playerTransform.position + playerTransform.up * 0.5f).y;
        }
        
        public static void ClimbStateFreeFallTest(PlayerEntity player, IAdaptiveContainer<IFsmInputCommand> commands)
        {
            if (!CanTestFreeFall(player) || OverlapCapsuleTest(player) || IsHitGround(player, ProbeDist)) return;

            var item = commands.GetAvailableItem(command => command.Type == FsmInput.None);
            item.Type = FsmInput.Freefall;
        }

        private static bool CanTestFreeFall(PlayerEntity player)
        {
            if (null == player || !player.hasThirdPersonAnimator) return false;
            var thirdPersonAnimator = player.thirdPersonAnimator.UnityAnimator;
            return thirdPersonAnimator.GetCurrentAnimatorStateInfo(0).IsName("ClimbEnd");
        }
 
         private static bool OverlapCapsuleTest(PlayerEntity playerEntity)
         {
             var gameObject = playerEntity.RootGo();
             var prevLayer = gameObject.layer;
 
             IntersectionDetectTool.SetColliderLayer(gameObject, UnityLayerManager.GetLayerIndex(EUnityLayerName.User));
 
             var overlapPos = gameObject.transform.position;
             PlayerEntityUtility.GetCapsule(playerEntity, overlapPos, out _capsuleBottom, out _capsuleUp,
                 out _capsuleRadius);
             var casts = Physics.OverlapCapsule(_capsuleBottom, _capsuleUp, _capsuleRadius,
                 UnityLayers.AllCollidableLayerMask);
 
             IntersectionDetectTool.SetColliderLayer(gameObject, prevLayer);
 
             return casts.Length > 0;
         }
 
         private static bool IsHitGround(PlayerEntity playerEntity, float dist)
         {
             var gameObject = playerEntity.RootGo();
             var prevLayer = gameObject.layer;
             IntersectionDetectTool.SetColliderLayer(gameObject, UnityLayerManager.GetLayerIndex(EUnityLayerName.User));
 
             var startPoint = gameObject.transform.position;
             startPoint.y += _capsuleRadius;
 
             RaycastHit outHit;
             var isHit = Physics.SphereCast(startPoint, _capsuleRadius, Vector3.down, out outHit , dist,
                 UnityLayers.AllCollidableLayerMask);
 
             IntersectionDetectTool.SetColliderLayer(gameObject, prevLayer);
 
             return isHit;
         }
     }
 }