using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VF.Builder;
using VF.Feature.Base;
using VF.Inspector;
using VF.Model.Feature;

namespace VF.Feature {
    /**
     * Zero out all blendshape normals if you cannot use "Legacy Blendshape Normals" for some reason.
     * This avoids strange lighting artifacts with incorrectly blended normals, but is less efficient on VRAM than the legacy import setting.
     * Will also reduce all blendshapes to a single blend frame.
     */
    public class ZeroBlendshapeNormalsBuilder : FeatureBuilder<ZeroBlendshapeNormals> {
        [FeatureBuilderAction(FeatureOrder.Default)]
        public void Apply() {
            var skinnedMeshRenderers = featureBaseObject.GetComponentsInSelfAndChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers) {
                var mesh = skinnedMeshRenderer.sharedMesh;
                if (mesh != null) {
                    skinnedMeshRenderer.sharedMesh = FixBlendshapeNormals(mesh);
                }
            }
        }

        private static Mesh FixBlendshapeNormals(Mesh input)
        {
            var mesh = MutableManager.MakeMutable(input);

            var blendshapes = new List<(string name, Vector3[] deltaVertices, Vector3[] deltaTangents)>();
            var discard = new Vector3[mesh.vertexCount];
            var zero = new Vector3[mesh.vertexCount];

            for (int i = 0; i < mesh.blendShapeCount; i++) {
                var name = mesh.GetBlendShapeName(i);
                var deltaVertices = new Vector3[mesh.vertexCount];
                var deltaTangents = new Vector3[mesh.vertexCount];
                mesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, discard, deltaTangents);
                blendshapes.Add((name, deltaVertices, deltaTangents));
            }

            mesh.ClearBlendShapes();

            foreach (var (name, deltaVertices, deltaTangents) in blendshapes) {
                mesh.AddBlendShapeFrame(name, 100, deltaVertices, zero, deltaTangents);
            }

            return mesh;
        }

        public override string GetEditorTitle() {
            return "Zero Blendshape Normals";
        }

        public override VisualElement CreateEditor(SerializedProperty prop) {
            return VRCFuryEditorUtils.Info(
                "Zero out all blendshape normals if you cannot use \"Legacy Blendshape Normals\" for some reason.\n" +
                "This avoids strange lighting artifacts with incorrectly blended normals, but is less efficient on VRAM than the legacy import setting.\n" +
                "Will also reduce all blendshapes to a single blend frame."
            );
        }
    }
}