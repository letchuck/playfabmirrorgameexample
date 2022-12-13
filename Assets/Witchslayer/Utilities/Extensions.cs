using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Witchslayer.Utilities
{

    public static class Extensions
    {

        public static void DestroyChildren(this Transform t, int amountToMaintain=-1, params Transform[] exceptions)
        {
            int count = amountToMaintain == -1 ? 0 : amountToMaintain;
            if (t.childCount <= count) return;

            if (Application.isPlaying)
            {
                for (int i = t.childCount - 1; i >= count; i--)
                {
                    Transform child = t.GetChild(i);

                    if (!exceptions.Contains(child))
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
#if UNITY_EDITOR
            else
            {
                for (int i = t.childCount - 1; i >= count; i--)
                {
                    Transform child = t.GetChild(i);

                    if (!exceptions.Contains(child))
                    {
                        // why is prefab workflow
                        if (PrefabUtility.IsPartOfPrefabInstance(t))
                        {
                            PrefabUtility.UnpackPrefabInstance(
                                PrefabUtility.GetOutermostPrefabInstanceRoot(t),
                                PrefabUnpackMode.Completely,
                                InteractionMode.AutomatedAction
                            );
                        }

                        GameObject.DestroyImmediate(child.gameObject);
                    }
                }
            }
#endif
        }

        [System.Obsolete("Just do (a&b)!=0")]
        public static bool HasFlags<T>(this T a, T b) where T : struct
        {
            if (!typeof(T).IsEnum) return false;
            if (!typeof(T).IsDefined(typeof(System.FlagsAttribute), false)) return false;

            return ((int)(object)a & (int)(object)b) != 0;
        }

        /// <summary>
        /// Replaces last item if at capacity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="item"></param>
        public static void AddOrReplaceLast<T>(this List<T> list, T item)
        {
            if (list.Count >= list.Capacity)
                list.RemoveAt(0);
            list.Add(item);
        }

    }

}
