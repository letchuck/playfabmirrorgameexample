using System.Collections.Generic;
using UnityEditor;
using Witchslayer.Utilities.Editor;

namespace Witchslayer.Chat.Editor
{

    /// <summary>
    /// Sets a custom define if PlayFab exists in the project since PlayFab doesn't include defines themselves for some reason. 
    /// </summary>
    public class PlayFabDefineSetter
    {

        private const string DEFINE = "USE_PLAYFAB";

        [InitializeOnLoadMethod]
        public static void Load()
        {
            // look for the asmdef since playfab doesnt
            string[] guids = AssetDatabase.FindAssets("t:asmdef");
            foreach (var item in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(item);
                if (path.ToLower().Contains("playfab.asmdef"))
                {
                    // set the define if it exists
                    SetDefine();
                    return;
                }
            }

            // didnt find the playfab asmdef, so remove the custom define if it exists
            RemoveDefine();
        }

        /// <summary>
        /// Removes the custom DEFINE for all build targets if the playfab SDK asmdef doesnt exist in the project.
        /// </summary>
        private static void RemoveDefine()
        {
            foreach (BuildTargetGroup group in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (!EditorUtils.IsValidBuildTargetGroup(group)) continue;

                List<string> defines = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
                if (defines.Contains(DEFINE))
                {
                    defines.Remove(DEFINE);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(';', defines));
                }
            }
        }

        /// <summary>
        /// Sets the custom define for all build targets if the playfab SDK asmdef exists in the project.
        /// </summary>
        private static void SetDefine()
        {
            foreach (BuildTargetGroup group in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (!EditorUtils.IsValidBuildTargetGroup(group)) continue;

                List<string> defines = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
                if (!defines.Contains(DEFINE))
                {
                    defines.Add(DEFINE);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(';', defines));
                }
            }
        }

    }

}
