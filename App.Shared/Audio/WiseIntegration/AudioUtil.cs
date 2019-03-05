using App.Shared.Audio;
using App.Shared.Sound;
using Core.Utils;
using System.IO;
using UnityEngine;
using WeaponConfigNs;
namespace App.Shared
{
    public static class AudioUtil
    {
        public static readonly LoggerAdapter AudioLogger = new LoggerAdapter(typeof(AKAudioDispatcher));
        public static void AssertProcessResult(AKRESULT result, string s, params object[] args)
        {
            if (!SharedConfig.IsServer)
            {
                s = string.Format(s, args);
                if (result != AKRESULT.AK_Success && result != AKRESULT.AK_BankAlreadyLoaded)
                {
                    DebugUtil.MyLog(s + string.Format(" {0} ", result), DebugUtil.DebugColor.Grey);
                    AudioLogger.Info(string.Format("[Audio Result Exception]{0}  {1}", s, result));
                }
                else
                {
                    AudioLogger.Info(s + string.Format(" {0} ", result));
                }
            }
        }
        public static bool Sucess(this AKRESULT result)
        {
            return result == AKRESULT.AK_Success || result == AKRESULT.AK_BankAlreadyLoaded;
        }

        public static AudioGrp_ShotModelIndex ToAudioGrpIndex(this EFireMode fireModel)
        {

            switch (fireModel)
            {
                case EFireMode.Auto:
                    return AudioGrp_ShotModelIndex.Continue;
                case EFireMode.Burst:
                    return AudioGrp_ShotModelIndex.Trriple;
                default:
                    return AudioGrp_ShotModelIndex.Single;
            }

        }
       // [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void NLog(string s, params object[] args)
        {
            if (!SharedConfig.IsServer)
            {
                s = string.Format(s, args);
                DebugUtil.MyLog(s, DebugUtil.DebugColor.Blue);
                AudioLogger.Info("[Audio Log] " + s);
            }

        }
        public static void ELog(string s, params object[] args)
        {
            if(!SharedConfig.IsServer)
            {
            s = string.Format(s, args);
            DebugUtil.MyLog(s, DebugUtil.DebugColor.Grey);
                AudioLogger.Info("[Audio Error] " + s);

            }
        }

        public static string[] GetBankAssetNamesByFolder(string folder)
        {
            try
            {
                string assetFolder = (string.IsNullOrEmpty(folder)) ? AkUtilities.GetWiseBankFolder_Full() : folder;
                var paths = Directory.GetFiles(assetFolder, "*.bnk", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < paths.Length; i++)
                    paths[i] = Path.GetFileName(paths[i]);
                return paths;
            }
            catch (System.Exception e)
            {

            }
            return null;

        }
        public static AudioGrp_Footstep ToFootState(this AudioEnvironmentSourceType sourceType)
        {
            switch(sourceType)
            {
                case AudioEnvironmentSourceType.Walk:
                    return AudioGrp_Footstep.Walk;
                case AudioEnvironmentSourceType.Squat:
                    return AudioGrp_Footstep.Squat;
                case AudioEnvironmentSourceType.Crawl:
                    return AudioGrp_Footstep.Crawl;
                //case AudioEnvironmentSourceType.Land:
                 //   return AudioGrp_Footstep.lan;
                default:
                    return AudioGrp_Footstep.None;
            }
            
        }


        public static AudioGrp_MatIndex ToAudioMatGrp(this TerrainMatOriginType matType)
        {
            switch(matType)
            {
          
                case TerrainMatOriginType.Dirt:
                    return AudioGrp_MatIndex.Concrete;
                case TerrainMatOriginType.Grass:
                    return AudioGrp_MatIndex.Grass;
                case TerrainMatOriginType.Rock:
                    return AudioGrp_MatIndex.Rock;
                case TerrainMatOriginType.Sand:
                    return AudioGrp_MatIndex.Sand;
                default:
                    return AudioGrp_MatIndex.Default;
            }
           
        }

    }
}
