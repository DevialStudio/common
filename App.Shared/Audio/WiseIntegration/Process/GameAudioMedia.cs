using App.Shared.Audio;
using App.Shared.Configuration;
using App.Shared.Terrains;
using Core;
using Core.Utils;
using System;
using UnityEngine;
using Utils.Configuration;
using Utils.Singleton;
using WeaponConfigNs;
using XmlConfig;

namespace App.Shared
{
    /// <summary>
    /// Defines the <see cref="GameAudioMedia" />
    /// </summary>
    public class GameAudioMedia
    {
        private static readonly LoggerAdapter audioLogger = new LoggerAdapter(typeof(AKAudioDispatcher));

        public static void PlayWeaponAudio(int weaponId, GameObject target, Func<AudioWeaponItem, int> propertyFilter)
        {
            if (SharedConfig.IsServer)
                return;
            AudioEventItem evtConfig = SingletonManager.Get<AudioWeaponManager>().FindById(weaponId, propertyFilter);
            if (evtConfig != null && AKAudioEntry.Dispatcher != null)
                AKAudioEntry.Dispatcher.PostEvent(evtConfig, target);
        }
        public static bool PlayEnvironmentAudio(AudioGrp_Footstep sourceType, Vector3 Position,GameObject target)
        {
            if (SharedConfig.IsServer || AKAudioEntry.Dispatcher == null)
                return false;
            if (sourceType == AudioGrp_Footstep.None) return false;
            int sceneId = SingletonManager.Get<MapConfigManager>().SceneParameters.Id;
            var terrain = SingletonManager.Get<TerrainManager>().GetTerrain(sceneId);
            TerrainMatOriginType matType = (TerrainMatOriginType)terrain.GetTerrainPositionMatType(Position);
            AudioGrp_MatIndex matGrpIndex = matType.ToAudioMatGrp();
            AudioEventItem evtConfig = SingletonManager.Get<AudioEventManager>().FindById(GlobalConst.AudioEvt_Footstep);
            AKAudioEntry.Dispatcher.SetSwitch(target, matGrpIndex);
            AKAudioEntry.Dispatcher.SetSwitch(target, sourceType);

            //  AKAudioEntry.Dispatcher.SetSwitch(target, matGrpIndex);
            //AKAudioEntry.Dispatcher.SetSwitch(target, matType);

            AKAudioEntry.Dispatcher.PostEvent(evtConfig, target);
            return true;
        }
        public static void PlayEnvironmentAudio(AudioEnvironmentSourceType sourceType,Vector3 Position, GameObject target)
        {
            if (SharedConfig.IsServer|| AKAudioEntry.Dispatcher == null)
                return;
                PlayEnvironmentAudio(sourceType.ToFootState(), Position, target);
        

            //public SoundConfigItem Convert(PlayerEntity playerEntity, EPlayerSoundType playerSoundType)
            //{
            //    switch ((EPlayerSoundType)playerSoundType)
            //    {
            //        case EPlayerSoundType.Walk:
            //            return ConvertTerrainSound(playerEntity, ETerrainSoundType.Walk);
            //        case EPlayerSoundType.Squat:
            //            return ConvertTerrainSound(playerEntity, ETerrainSoundType.Squat);
            //        case EPlayerSoundType.Crawl:
            //            return ConvertTerrainSound(playerEntity, ETerrainSoundType.Crawl);
            //        case EPlayerSoundType.Land:
            //            return ConvertTerrainSound(playerEntity, ETerrainSoundType.Land);
            //        default:
            //            return ConvertPlayerSound(playerEntity, playerSoundType);
            //    }
            //}
        }
        /// <summary>
        /// 枪械切换
        /// </summary>
        /// <param name="weaponCfg"></param>
        public static void SwitchFireModelAudio(EFireMode model, GameObject target)
        {
            if (SharedConfig.IsServer)
                return;
#if UNITY_EDITOR
            if (AudioInfluence.IsForbidden) return;
#endif
           
            AudioGrp_ShotModelIndex shotModelIndex = model.ToAudioGrpIndex();
            if (AKAudioEntry.Dispatcher != null)
                AKAudioEntry.Dispatcher.SetSwitch(target, shotModelIndex);
        }

        public static void PostAutoRegisterGameObjAudio(Vector3 position, bool createObject)
        {
        }
    }
}
