#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using ResoniteModLoader;
using UnityEngine;
using UnityFrooxEngineRunner;
using System.IO.MemoryMappedFiles;
using Elements.Assets;

#endregion

namespace Thundagun;

public class Thundagun : ResoniteMod
{
    public const string AuthorString = "Fro Zen, 989onan, DoubleStyx, Nytra, Merith-TK, SectOLT"; // in order of first commit
    public const string VersionString = "1.2.0"; // change minor version for config "API" changes

    public static MemoryMappedFile MemoryFrooxEngine;

    public override string Name => "Thundagun";
    public override string Author => AuthorString;
    public override string Version => VersionString;
    public override string Link => "https://github.com/Frozenreflex/Thundagun";


    public override void OnEngineInit()
    {
        var harmony = new Harmony("com.frozenreflex.Thundagun");




        //PipeSecurity sec = new PipeSecurity();
        // = new FileStream();
        Thundagun.MemoryFrooxEngine = MemoryMappedFile.OpenExisting("FrooxEnginePipe"); //= new NamedPipeServerStream("FrooxEnginePipe", PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.None, 0, 20000000);// .CreateOrOpen("FrooxEngineMemoryMap", 25000, MemoryMappedFileAccess.Write);
                                                                                        //try
                                                                                        //{
                                                                                        //    Thundagun.MemoryFrooxEngine
        harmony.PatchAll();

        //}
        //catch
        //{
        //    Thundagun.Msg("no need to wait nerd!");
        //}
    }
}



[HarmonyPatch(typeof(FrooxEngineRunner))]
public static class FrooxEngineRunnerPatch
{

    
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void Update(FrooxEngineRunner __instance,
        ref Engine ____frooxEngine, ref bool ____shutdownRequest, ref Stopwatch ____externalUpdate,
        ref World ____lastFocusedWorld,
        ref HeadOutput ____vrOutput, ref HeadOutput ____screenOutput, ref AudioListener ____audioListener,
        ref List<World> ____worlds)
    {
        try
        {
            MemoryObjectManagement.Release();
        }
        catch (System.Exception e)
        {
            Thundagun.Msg(e.Message.ToString());
            Thundagun.Msg(e.StackTrace.ToString());
        }
    }

}


public enum MeshUploadBlenderHint
{
    Geometry = 1 << 0,
    Bones = 1 << 1,
}
/*
[HarmonyPatch(typeof(MeshRendererConnectorBase<FrooxEngine.SkinnedMeshRenderer, UnityEngine.SkinnedMeshRenderer>))]
public static class SkinnedMeshConnectorPatch
{
    [HarmonyPatch("ApplyChanges")]
    [HarmonyPostfix]
    public static void ApplyChanges(MeshRendererConnectorBase<FrooxEngine.SkinnedMeshRenderer, UnityEngine.SkinnedMeshRenderer> __instance)
    {
        ulong[] bones = __instance.Owner.Bones.ToList().Select(x => x.ReferenceID.Position).ToArray();
        MeshConnectorPatch.WriteDataToBuffer((MeshConnector)__instance.Owner.Mesh.Asset.Connector, false, true, __instance.Owner.ReferenceID.Position, __instance.Owner.Slot.ReferenceID.Position, bones);
    }

    
}


[HarmonyPatch(typeof(MeshRendererConnectorBase<FrooxEngine.MeshRenderer, UnityEngine.MeshRenderer>))]
public static class MeshRendererConnectorBasePatch
{
    [HarmonyPatch("ApplyChanges")]
   
    [HarmonyPostfix]
    public static void ApplyChanges(MeshRendererConnectorBase<FrooxEngine.MeshRenderer, UnityEngine.MeshRenderer> __instance)
    {
        MeshConnectorPatch.WriteDataToBuffer((MeshConnector)__instance.Owner.Mesh.Asset.Connector, false, true, __instance.Owner.ReferenceID.Position, __instance.Owner.Slot.ReferenceID.Position, new ulong[] { });
    }
    
}

public class MeshConnectorExtension
{
    public MeshX savedMesh = null;

    public bool upload = false;
    public MeshConnectorExtension(MeshX meshx, MeshUploadHint uploadHint)
    {
        savedMesh = meshx;
        if (uploadHint[MeshUploadHint.Flag.Geometry])
        {
            this.upload = true;
        }
    }
}*/

public class SlotConnectorExtension
{

    public string Name = "";
    public string NewName = "";

    public ulong IDposition = 0;

    public SlotConnectorExtension()
    {

    }

}


[Flags]
public enum SlotTransferType
{

    RefID = 1 << 0,
    Destroy = 1 << 1,
    Create = 1 << 2,
    Name = 1 << 3,
    Active = 1 << 4,
    Position = 1 << 5,
    Rotation = 1 << 6,
    Scale = 1 << 7,
    Parent = 1 << 8
}

[HarmonyPatch(typeof(UnityFrooxEngineRunner.SlotConnector))]
public static class SlotConnectorPatch
{
    public static readonly byte TYPE = 1;

    public static Dictionary<SlotConnector, SlotConnectorExtension> memory = new();


    [HarmonyPatch("GenerateGameObject")]
    [HarmonyPostfix]
    private static void GenerateGameObject(SlotConnector __instance)
    {
        SlotConnectorPatch.WriteDataToBuffer(__instance, SlotTransferType.Parent | SlotTransferType.Create | SlotTransferType.Active, false, true);
    }

    [HarmonyPatch("SetData")]
    [HarmonyPostfix]
    private static void SetData(SlotConnector __instance)
    {
        SlotConnectorPatch.WriteDataToBuffer(__instance,SlotTransferType.Rotation | SlotTransferType.Position | SlotTransferType.Scale | SlotTransferType.Active, false, true);
    }

    [HarmonyPatch("TryDestroy")]
    [HarmonyPostfix]
    public static void TryDestroy(SlotConnector __instance)
    {
        SlotConnectorPatch.WriteDataToBuffer(__instance, SlotTransferType.Destroy, true, false);
    }


    public static void WriteDataToBuffer(SlotConnector __instance, SlotTransferType type, bool destroy, bool create)
    {
        
        try
        {
            memory.TryGetValue(__instance, out SlotConnectorExtension memoryobj);
            if (memoryobj == null)
            {
                memory.Add(__instance, memoryobj = new SlotConnectorExtension());
            }
            type |= SlotTransferType.RefID;
            try
            {
                memoryobj.NewName = __instance.Owner.Name;
                if (!memoryobj.Name.Equals(memoryobj.NewName)) { type |= SlotTransferType.Name; }
            }
            catch
            {
                //idc
            }
            if (type.HasFlag(SlotTransferType.Destroy))
            {

                type = SlotTransferType.Destroy;
                type |= SlotTransferType.RefID;
            }

            MemoryObjectManagement.Save(TYPE);
            MemoryObjectManagement.Save(((int)type));
            



            

            if (type.HasFlag(SlotTransferType.RefID))
            {
                try
                {
                    MemoryObjectManagement.Save(__instance.Owner.ReferenceID.Position);
                }
                catch
                {
                    ulong refid = 0;
                    MemoryObjectManagement.Save(refid);
                }

            }
            if (type.HasFlag(SlotTransferType.Destroy))
            {
                MemoryObjectManagement.Save(destroy);

            }

            if (type.HasFlag(SlotTransferType.Create))
            {
                MemoryObjectManagement.Save(create);
            }






            if (type.HasFlag(SlotTransferType.Name))
            {
                memoryobj.Name = memoryobj.NewName;

                try
                {
                    if (memoryobj.Name.Equals("FINIS_FR"))
                    {
                        memoryobj.Name = "NICE TRY";
                    }
                    MemoryObjectManagement.SaveString(memoryobj.Name);
                }
                catch
                {
                    string name = "UNNAMED";
                    MemoryObjectManagement.SaveString(name);
                }
            }


            if (type.HasFlag(SlotTransferType.Active))
            {
                MemoryObjectManagement.Save(__instance.Owner.ActiveSelf);
            }
            if (type.HasFlag(SlotTransferType.Position))
            {
                MemoryObjectManagement.Save(__instance.Owner.LocalPosition.x);
                MemoryObjectManagement.Save(__instance.Owner.LocalPosition.y);
                MemoryObjectManagement.Save(__instance.Owner.LocalPosition.z);
            }
            if (type.HasFlag(SlotTransferType.Rotation))
            {
                MemoryObjectManagement.Save(__instance.Owner.LocalRotation.x);
                MemoryObjectManagement.Save(__instance.Owner.LocalRotation.y);
                MemoryObjectManagement.Save(__instance.Owner.LocalRotation.z);
                MemoryObjectManagement.Save(__instance.Owner.LocalRotation.w);
            }
            if (type.HasFlag(SlotTransferType.Scale))
            {
                MemoryObjectManagement.Save(__instance.Owner.LocalScale.x);
                MemoryObjectManagement.Save(__instance.Owner.LocalScale.y);
                MemoryObjectManagement.Save(__instance.Owner.LocalScale.z);
            }
            if (type.HasFlag(SlotTransferType.Parent))
            {
                try
                {
                    MemoryObjectManagement.Save(__instance.Owner.Parent.Connector.Owner.ReferenceID.Position);
                }
                catch
                {
                    MemoryObjectManagement.Save((ulong)0);
                }
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

/*
[HarmonyPatch(typeof(UnityFrooxEngineRunner.MeshConnector))]
public static class MeshConnectorPatch
{


    

    public static Dictionary<UnityFrooxEngineRunner.MeshConnector, MeshConnectorExtension> meshes = new();//this.... is horrible I am sorry

    [HarmonyPatch("UpdateMeshData")]
    [HarmonyPostfix]
    public static void UpdateMeshData(UnityFrooxEngineRunner.MeshConnector __instance, ref MeshX ____meshx, ref MeshUploadHint ____uploadHint)
    {
        meshes.Replace(__instance, new MeshConnectorExtension(____meshx, ____uploadHint));
    }

    public static readonly byte TYPE = 2;


    public static void WriteDataToBuffer(UnityFrooxEngineRunner.MeshConnector __instance, bool destroy, bool create, ulong meshid, ulong slotrefid, ulong[] bones)
    {
        
        try
        {

            meshes.TryGetValue(__instance, out MeshConnectorExtension memoryobj);
            if (memoryobj == null)
            {
                return;
            }
            MeshX Mesh = memoryobj.savedMesh;
            bool upload = memoryobj.upload;
            if (Mesh.HasBoneBindings)
            {
                if (bones.Length == 0)
                {
                    return;
                }
            }

            if (upload)
            {
                memoryobj.upload = false;
                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(slotrefid);
                MemoryObjectManagement.Save(meshid);
                MeshUploadBlenderHint flags = MeshUploadBlenderHint.Geometry;
                if (Mesh.HasBoneBindings)
                {
                    flags |= MeshUploadBlenderHint.Bones;
                }


                MemoryObjectManagement.Save((byte)flags);
                float[] positions = new float[Mesh.Vertices.Count() * 3];
                int j = 0;
                foreach (Vertex vert in Mesh.Vertices)
                {
                    positions[3 * j] = vert.Position.x;
                    positions[3 * j + 1] = vert.Position.y;
                    positions[3 * j + 2] = vert.Position.z;
                    j++;
                }
                MemoryObjectManagement.SaveArray(positions);

                int[] tris = new int[Mesh.Triangles.Count() * 3];
                j = 0;
                foreach (Triangle tri in Mesh.Triangles)
                {
                    tris[3 * j] = tri.Vertex0Index;
                    tris[3 * j + 1] = tri.Vertex1Index;
                    tris[3 * j + 2] = tri.Vertex2Index;
                    j++;
                }
                MemoryObjectManagement.SaveArray(tris);



                if (Mesh.HasBoneBindings)
                {
                    MemoryObjectManagement.SaveArray(bones);
                    Thundagun.Msg("BONES LENGTH IS:" + bones.Length.ToString());
                    int[] boneindices = new int[Mesh.Vertices.Count() * 4];

                    j = 0;
                    foreach (BoneBinding o in Mesh.RawBoneBindings)
                    {
                        boneindices[4 * j] = o.boneIndex0;
                        boneindices[4 * j + 1] = o.boneIndex1;
                        boneindices[4 * j + 2] = o.boneIndex2;
                        boneindices[4 * j + 3] = o.boneIndex3;
                        j++;
                    }
                    MemoryObjectManagement.SaveArray(boneindices);

                    float[] boneweights = new float[Mesh.Vertices.Count() * 4];

                    j = 0;
                    foreach (BoneBinding o in Mesh.RawBoneBindings)
                    {
                        boneweights[4 * j] = o.weight0;
                        boneweights[4 * j + 1] = o.weight1;
                        boneweights[4 * j + 2] = o.weight2;
                        boneweights[4 * j + 3] = o.weight3;
                        j++;
                    }
                    MemoryObjectManagement.SaveArray(boneweights);
                    float[] bone_pos = new float[Mesh.BoneCount * 3];

                    j = 0;
                    foreach (Bone o in Mesh.Bones)
                    {
                        bone_pos[3 * j] = o.BindPose.Inverse.DecomposedPosition.x;

                        bone_pos[3 * j + 1] = o.BindPose.Inverse.DecomposedPosition.y;
                        bone_pos[3 * j + 2] = o.BindPose.Inverse.DecomposedPosition.z;
                        j++;
                    }
                    MemoryObjectManagement.SaveArray(bone_pos);

                    float[] bone_vector = new float[Mesh.BoneCount * 3];

                    j = 0;
                    foreach (Bone o in Mesh.Bones)
                    {
                        float3 pointing = o.BindPose.Inverse.DecomposedRotation * new float3(0, 0, 1);
                        bone_vector[3 * j] = pointing.x;
                        bone_vector[3 * j + 1] = pointing.y;
                        bone_vector[3 * j + 2] = pointing.z;
                        j++;
                    }

                    MemoryObjectManagement.SaveArray(bone_vector);

                }
                MemoryObjectManagement.ReleaseObject();

            }




            //TODO: IMPLEMENT THIS - @989onan
            //MemoryObjectManagement.Save(bones != null ? savedMesh.BoneCount : 0);
            //savedMesh.Bones.ToList().ForEach(bone => MemoryObjectManagement.SaveString(bone.Name));






            //TODO: Shove this in via an array of arrays and then decode and numpy shape it on the Python side. who cares!
            //List<int> indices = new List<int>();
            //indices.AddRange();// np.reshape(np.array()[:], (submesh.Count, submesh.IndiciesPerElement))
            //for (int i = 0; i < meshx.SubmeshCount; i++)
            //{
            //    submesh = meshx.GetSubmesh(i);
            //    /indices =  (, np.reshape(np.array()[:submesh.Count * submesh.IndiciesPerElement], (submesh.Count, submesh.IndiciesPerElement))), axis = 0)
            //}


            //mesh_data.from_pydata(np.reshape(np.array(v[3]), (-1, 3)), [], indices)
            //mesh_finishes.append(v[2])
        }
        catch (System.Exception e)
        {
            Thundagun.Msg(e.Message.ToString());
            Thundagun.Msg(e.StackTrace.ToString());
            MemoryObjectManagement.Purge();
        }
    }

}*/


