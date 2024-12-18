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

namespace Thundagun
{
    public enum MeshUploadBlenderHint
    {
        Geometry = 1 << 0,
        Bones = 1 << 1,
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
                MeshConnectorPatch.WriteDataToBuffer((MeshConnector)__instance.Owner.Mesh.Asset.Connector, __instance.Owner.ReferenceID.Position, __instance.Owner.Slot.ReferenceID.Position, bones);
            }
            catch (System.Exception e)
            {
                Thundagun.Msg("Skinned Mesh RendererConnectorPatch Error");
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
        public float[] bone_pos = new float[] { 0 };
        public float[] bone_vector = new float[] { 0 };
        public bool bone_data = false;
        public MeshConnectorExtension()
        {
            
        }


        public void Update(MeshUploadHint uploadHint, MeshX meshx)
        {
            if (uploadHint[MeshUploadHint.Flag.Geometry])
            {
                this.upload = true;
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

                bone_pos = new float[meshx.BoneCount * 3];

                j = 0;
                foreach (Elements.Assets.Bone o in meshx.Bones)
                {
                    bone_pos[3 * j] = o.BindPose.Inverse.DecomposedPosition.x;

                    bone_pos[3 * j + 1] = o.BindPose.Inverse.DecomposedPosition.y;
                    bone_pos[3 * j + 2] = o.BindPose.Inverse.DecomposedPosition.z;
                    j++;
                }


                bone_vector = new float[meshx.BoneCount * 3];

                j = 0;
                foreach (Elements.Assets.Bone o in meshx.Bones)
                {
                    float3 pointing = o.BindPose.Inverse.DecomposedRotation * new float3(0, 0, 1);
                    bone_vector[3 * j] = pointing.x;
                    bone_vector[3 * j + 1] = pointing.y;
                    bone_vector[3 * j + 2] = pointing.z;
                    j++;
                }



            }
        }
    }

    
    [HarmonyPatch(typeof(UnityFrooxEngineRunner.MeshConnector))]
    public static class MeshConnectorPatch
    {


    

        public static Dictionary<UnityFrooxEngineRunner.MeshConnector, MeshConnectorExtension> meshes = new();//this.... is horrible I am sorry

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
                meshes.Add(__instance, new MeshConnectorExtension());
                meshes[__instance].Update(uploadHint, meshx);
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

                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(slotrefid);
                MemoryObjectManagement.Save(meshid);
                MeshUploadBlenderHint flags = MeshUploadBlenderHint.Geometry;
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
                    MemoryObjectManagement.SaveArray(memoryobj.bone_pos);
                    MemoryObjectManagement.SaveArray(memoryobj.bone_vector);
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
