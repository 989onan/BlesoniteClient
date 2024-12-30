using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;
using UnityFrooxEngineRunner;
using static FrooxEngine.DynamicBoneChain;
using static HarmonyLib.Code;

namespace Thundagun
{
    public enum MeshUploadBlenderHint
    {
        Geometry = 1 << 0,
        Bones = 1 << 1,
        NewObj = 1 << 2,
    }
    
    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public static class SkinnedMeshConnectorPatch
    {
        [HarmonyPatch("OnUpdateRenderer")]
        [HarmonyPostfix]
        public static void OnUpdateRenderer(SkinnedMeshRendererConnector __instance)
        {
            try {
                if (__instance.Owner.Mesh.Target == null || !__instance.Owner.Mesh.IsAssetAvailable)
                {
                    return;
                }
                ulong[] bones = __instance.Owner.Bones.ToList().Select(x => x.ReferenceID.Position).ToArray();

                __instance.Owner.Bones.ToList().ForEach(x => SlotPatch.EnsureMemObj(x));
                SlotPatch.getmemobj(__instance.Owner.Slot);
                MeshConnectorPatch.WriteDataToBuffer((MeshConnector)__instance.Owner.Mesh.Asset.Connector, __instance.Owner.ReferenceID.Position, __instance.Owner.Slot.ReferenceID.Position, bones);
            }
            catch (System.Exception e)
            {
                Thundagun.Msg("Skinned Mesh RendererConnectorPatch Error");
                Thundagun.Msg(e.Message.ToString());
                Thundagun.Msg(e.StackTrace.ToString());
            }
        }


        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        public static void Destroy(SkinnedMeshRendererConnector __instance)
        {
            try
            {
                __instance.Owner.Bones.ToList().ForEach(x => {if (x != null) { SlotPatch.DestroySlot(x);}});
            }
            catch (System.Exception e)
            {
                Thundagun.Msg("Skinned Mesh RendererConnectorPatch DESTROY Error");
                Thundagun.Msg(e.Message.ToString());
                Thundagun.Msg(e.StackTrace.ToString());
            }
        }
    }

    /*
    [HarmonyPatch(typeof(MeshRendererConnector))]
    public static class MeshRendererConnectorPatch
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPostfix]
        public static void ApplyChanges(MeshRendererConnector __instance)
        {
            try
            {
                if (__instance.Owner.Mesh.Target == null || !__instance.Owner.Mesh.IsAssetAvailable)
                {
                    return;
                }
                MeshConnectorPatch.WriteDataToBuffer((MeshConnector)__instance.Owner.Mesh.Asset.Connector, __instance.Owner.ReferenceID.Position, __instance.Owner.Slot.ReferenceID.Position, new ulong[] { });
            }
            catch (System.Exception e)
            {
                Thundagun.Msg("Normal Mesh RendererConnectorPatch Error");
                Thundagun.Msg(e.Message.ToString());
                Thundagun.Msg(e.StackTrace.ToString());
            }
        }

    }*/
    

    public class MeshConnectorExtension
    {
        public MeshX savedMesh = null;

        public bool upload = false;
        public float[] positions = new float[] { 0 };

        public int[] tris = new int[] { 0 };
        public int[] boneindices = new int[] { 0,0,0,0 };
        public float[] boneweights = new float[] { 0 };
        public float[] bone_matrices = new float[] { 0 };
        public bool bone_data = false;
        public MeshConnectorExtension()
        {
            
        }


        public void Update(MeshUploadHint uploadHint, MeshX meshx)
        {
            if (uploadHint[MeshUploadHint.Flag.Geometry])
            {
                upload = true;

    }
            else
            {
                return;
            }

            positions = new float[meshx.Vertices.Count() * 3];
            int j = 0;
            foreach (Vertex vert in meshx.Vertices)
            {
                positions[3 * j] = vert.Position.x;
                positions[3 * j + 1] = vert.Position.y;
                positions[3 * j + 2] = vert.Position.z;
                j++;
            }

            tris = new int[meshx.Triangles.Count() * 3];
            j = 0;
            foreach (Triangle tri in meshx.Triangles)
            {
                tris[3 * j] = tri.Vertex0Index;
                tris[3 * j + 1] = tri.Vertex1Index;
                tris[3 * j + 2] = tri.Vertex2Index;
                j++;
            }

            if (meshx.HasBoneBindings)
            {
                bone_data = true;
                boneindices = new int[meshx.Vertices.Count() * 4];

                j = 0;
                foreach (BoneBinding o in meshx.RawBoneBindings)
                {
                    boneindices[4 * j] = o.boneIndex0;
                    boneindices[4 * j + 1] = o.boneIndex1;
                    boneindices[4 * j + 2] = o.boneIndex2;
                    boneindices[4 * j + 3] = o.boneIndex3;
                    j++;
                }


                boneweights = new float[meshx.Vertices.Count() * 4];

                j = 0;
                foreach (BoneBinding o in meshx.RawBoneBindings)
                {
                    boneweights[4 * j] = o.weight0;
                    boneweights[4 * j + 1] = o.weight1;
                    boneweights[4 * j + 2] = o.weight2;
                    boneweights[4 * j + 3] = o.weight3;
                    j++;
                }

                bone_matrices = new float[meshx.BoneCount * 16];




                j = 0;
                foreach (Elements.Assets.Bone o in meshx.Bones)
                {
                   

                    float4x4 final = o.BindPose.Inverse;
                    floatQ rotation = final.DecomposedRotation;
                    float3 scale = final.DecomposedScale;
                    float3 position = final.DecomposedPosition;

                    final = float4x4.Transform(new float3(position.x, position.z, position.y), new floatQ(rotation.x, rotation.z, rotation.y, -rotation.w), new float3(scale.x, scale.z, scale.y));

                    bone_matrices[16 * j + 0] = final.m00;
                    bone_matrices[16 * j + 1] = final.m01;
                    bone_matrices[16 * j + 2] = final.m02;
                    bone_matrices[16 * j + 3] = final.m03;

                    bone_matrices[16 * j + 4] = final.m10;
                    bone_matrices[16 * j + 5] = final.m11;
                    bone_matrices[16 * j + 6] = final.m12;
                    bone_matrices[16 * j + 7] = final.m13;

                    bone_matrices[16 * j + 8] = final.m20;
                    bone_matrices[16 * j + 9] = final.m21;
                    bone_matrices[16 * j + 10] = final.m22;
                    bone_matrices[16 * j + 11] = final.m23;
                    
                    bone_matrices[16 * j + 12] = final.m30;
                    bone_matrices[16 * j + 13] = final.m31;
                    bone_matrices[16 * j + 14] = final.m32;
                    bone_matrices[16 * j + 15] = final.m33;
                    j++;
                }
            }

            /*if (positions != prevpositions ||
                    tris != prevtris ||
                    boneindices != prevboneindices ||
                    boneweights != prevboneweights ||
                    bone_matrices != prevbone_matrices
                    )
            {
                prevpositions = positions;
                prevtris = tris;
                prevboneindices = boneindices;
                prevboneweights = boneweights;
                prevbone_matrices = bone_matrices;
                this.upload = true;
            }*/
        }
    }

    
    [HarmonyPatch(typeof(UnityFrooxEngineRunner.MeshConnector))]
    public static class MeshConnectorPatch
    {


    

        public static Dictionary<UnityFrooxEngineRunner.MeshConnector, MeshConnectorExtension> meshes = new Dictionary<UnityFrooxEngineRunner.MeshConnector, MeshConnectorExtension>();//this.... is horrible I am sorry

        [HarmonyPatch("UpdateMeshData")]
        [HarmonyPostfix]
        public static void UpdateMeshData(UnityFrooxEngineRunner.MeshConnector __instance, MeshX meshx, MeshUploadHint uploadHint)
        {
            try
            {

                if (meshes.ContainsKey(__instance))
                {
                    meshes[__instance].Update(uploadHint, meshx);
                    return;
                }
                MeshConnectorExtension new_mesh = new MeshConnectorExtension();
                new_mesh.Update(uploadHint, meshx);
                meshes.Add(__instance, new_mesh);
            }
            catch (System.Exception e)
            {
                Thundagun.Msg(e.Message.ToString());
                Thundagun.Msg(e.StackTrace.ToString());
            }
        }

        public static readonly byte TYPE = 2;


        public static void WriteDataToBuffer(UnityFrooxEngineRunner.MeshConnector __instance, ulong meshid, ulong slotrefid, ulong[] bones)
        {
        
            try
            {
                if (!meshes.TryGetValue(__instance, out MeshConnectorExtension memoryobj))
                {
                    return;
                }
                if (memoryobj.bone_data)
                {
                    if (bones.Length == 0)
                    {
                        return;
                    }
                }
                MeshUploadBlenderHint flags;
                if (!memoryobj.upload)
                {
                    MemoryObjectManagement.Save(TYPE);
                    MemoryObjectManagement.Save(slotrefid);
                    MemoryObjectManagement.Save(meshid);
                    flags = MeshUploadBlenderHint.NewObj;
                    MemoryObjectManagement.Save((byte)flags);
                    MemoryObjectManagement.ReleaseObject();
                    return;
                }
                memoryobj.upload = false;
                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(slotrefid);
                MemoryObjectManagement.Save(meshid);
                flags = MeshUploadBlenderHint.Geometry;
                if (memoryobj.bone_data)
                {
                    flags |= MeshUploadBlenderHint.Bones;
                }


                MemoryObjectManagement.Save((byte)flags);
                    
                MemoryObjectManagement.SaveArray(memoryobj.positions);

                    
                MemoryObjectManagement.SaveArray(memoryobj.tris);

                if (memoryobj.bone_data)
                {
                    MemoryObjectManagement.SaveArray(bones);
                    Thundagun.Msg("BONES LENGTH IS:" + bones.Length.ToString());

                    MemoryObjectManagement.SaveArray(memoryobj.boneindices);
                    MemoryObjectManagement.SaveArray(memoryobj.boneweights);
                    MemoryObjectManagement.SaveArray(memoryobj.bone_matrices);
                }

                MemoryObjectManagement.ReleaseObject();
            }
            catch (System.Exception e)
            {
                Thundagun.Msg(e.Message.ToString());
                Thundagun.Msg(e.StackTrace.ToString());
                MemoryObjectManagement.Purge();
            }
        }
    }
}
