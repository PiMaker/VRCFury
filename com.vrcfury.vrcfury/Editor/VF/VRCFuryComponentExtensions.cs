using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VF.Component;
using VF.Upgradeable;

namespace VF {
    public static class VRCFuryComponentExtensions {
        private static readonly HashSet<string> reimported = new HashSet<string>();

        /**
         * 
         * Unity doesn't try to re-deserialize assets after updating vrcfury, leaving components in a broken state.
         * If we find a broken component, schedule a reimport of it to try and resolve the issue.
         */
        private static void DelayReimport(VRCFuryComponent c) {
            string GetPath() {
                if (c == null) return null;
                var path = AssetDatabase.GetAssetPath(c);
                if (reimported.Contains(path)) return null;
                return path;
            }

            if (GetPath() == null) return;
            EditorApplication.delayCall += () => {
                if (!c.IsBroken()) return;
                var path = GetPath();
                if (path == null) return;
                reimported.Add(path);
                Debug.Log("Reimporting VRCFury asset that unity thinks is corrupted: " + path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            };
        }
        
        public static void Upgrade(this VRCFuryComponent c) {
            if (c.IsBroken()) return;
            if (PrefabUtility.IsPartOfPrefabInstance(c)) return;
            if (IUpgradeableUtility.UpgradeRecursive(c)) {
                if (c != null) EditorUtility.SetDirty(c);
            }
        }
        
        public static bool IsBroken(this VRCFuryComponent c) {
            return c.GetBrokenMessage() != null;
        }
        public static string GetBrokenMessage(this VRCFuryComponent c) {
            if (IUpgradeableUtility.IsTooNew(c)) {
                DelayReimport(c);
                return $"This component was created with a newer version of VRCFury";
            }
            
            var containsNull = false;
            UnitySerializationUtils.Iterate(c, visit => {
                if (visit.field?.Name == "content" && c.Version >= 0 && c.Version <= 2) {
                    // Allow VRCFury content field to be null until it's upgraded
                    return UnitySerializationUtils.IterateResult.Continue;
                }
                containsNull |=
                    visit.field?.GetCustomAttribute<SerializeReference>() != null
                    && visit.value == null;
                return UnitySerializationUtils.IterateResult.Continue;
            });
            if (containsNull) {
                DelayReimport(c);
                if (Application.unityVersion.StartsWith("2019")) {
                    if (c.unityVersion != null && c.unityVersion.StartsWith("2022")) {
                        return "This VRCFury asset was created using Unity 2022, which means it cannot be used on Unity 2019";
                    } else {
                        return "This VRCFury asset was probably created using Unity 2022, which means it cannot be used on Unity 2019";
                    }
                }
                return "Found a null SerializeReference";
            }
            return null;
        }
    }
}
