using UnityEditor;

namespace Witchslayer.Utilities.Editor
{

    public class EditorUtils 
    {

#pragma warning disable 0618
        public static bool IsValidBuildTargetGroup(BuildTargetGroup group)
        {
            if (group == BuildTargetGroup.Unknown
                || group == BuildTargetGroup.WP8)
                return false;

            // removed in 5.3
#if UNITY_5_3_OR_NEWER
            if (group == BuildTargetGroup.PSM)
                return false;
#endif

            // removed in 5.4
#if UNITY_5_4_OR_NEWER
            if ((int)group == 2 // webplayer
                || group == BuildTargetGroup.BlackBerry)
                return false;
#endif

            // removed in 5.5
#if UNITY_5_4_OR_NEWER
            if (group == BuildTargetGroup.PS3
                || group == BuildTargetGroup.XBOX360)
                return false;
#endif

            // removed in 2017.3
#if UNITY_2017_3_OR_NEWER
            if (group == BuildTargetGroup.Tizen
                || group == BuildTargetGroup.SamsungTV)
                return false;
#endif

            // removed in 2018.1
#if UNITY_2018_1_OR_NEWER
            if (group == BuildTargetGroup.N3DS
                || group == BuildTargetGroup.WiiU)
                return false;
#endif

            // removed in 2018.3
#if UNITY_2018_3_OR_NEWER
            if (group == BuildTargetGroup.PSP2)
                return false;
#endif

            // removed in 2019.3
#if UNITY_2019_3_OR_NEWER
            if (group == BuildTargetGroup.Facebook)
                return false;
#endif


            return true;
        }
#pragma warning restore 0618

    }

}
