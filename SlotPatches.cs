using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityFrooxEngineRunner;

namespace Thundagun
{

    public class SlotConnectorExtension
    {

        public string Name = "";
        public string NewName = "";

        public float3 prevposition = new();
        public float3 prevscale = new();
        public floatQ prevrotation = new();
        public bool prevactive = true;

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

        public static SlotConnectorExtension getmemobj(SlotConnector __instance)
        {
            memory.TryGetValue(__instance, out SlotConnectorExtension memoryobj);
            if (memoryobj == null)
            {
                memory.Add(__instance, memoryobj = new SlotConnectorExtension());
            }
            return memoryobj;


        }

        [HarmonyPatch("GenerateGameObject")]
        [HarmonyPostfix]
        private static void GenerateGameObject(SlotConnector __instance)
        {
            SlotConnectorPatch.WriteDataToBuffer(__instance, SlotTransferType.Rotation | SlotTransferType.Name | SlotTransferType.Position | SlotTransferType.Scale | SlotTransferType.Create | SlotTransferType.Active, getmemobj(__instance));
        }

        [HarmonyPatch("UpdateParent")]
        [HarmonyPostfix]
        private static void UpdateParent(SlotConnector __instance)
        {
            SlotConnectorPatch.WriteDataToBuffer(__instance, SlotTransferType.Parent, getmemobj(__instance));
        }

        [HarmonyPatch("UpdateData")]
        [HarmonyPostfix]
        private static void UpdateData(SlotConnector __instance)
        {
            SlotConnectorExtension memoryobj = getmemobj(__instance);

            SlotTransferType type = SlotTransferType.RefID;
            if (__instance.Owner.LocalPosition != memoryobj.prevposition)
            {
                memoryobj.prevposition = __instance.Owner.LocalPosition;
                type |= SlotTransferType.Position;
            }
            if (__instance.Owner.LocalRotation != memoryobj.prevrotation)
            {
                memoryobj.prevrotation = __instance.Owner.LocalRotation;
                type |= SlotTransferType.Rotation;
            }
            if (__instance.Owner.LocalScale != memoryobj.prevscale)
            {
                memoryobj.prevscale = __instance.Owner.LocalScale;
                type |= SlotTransferType.Scale;
            }

            if (__instance.Owner.ActiveSelf != memoryobj.prevactive)
            {
                memoryobj.prevactive = __instance.Owner.ActiveSelf;
                type |= SlotTransferType.Active;
            }

            SlotConnectorPatch.WriteDataToBuffer(__instance, type, memoryobj);
        }

        [HarmonyPatch("TryDestroy")]
        [HarmonyPostfix]
        public static void TryDestroy(SlotConnector __instance, bool ___shouldDestroy, ref int ___gameObjectRequests)
        {
            if (___shouldDestroy && ___gameObjectRequests == 0)
            {
                SlotConnectorPatch.WriteDataToBuffer(__instance, SlotTransferType.Destroy, getmemobj(__instance));
            }
        }


        public static void WriteDataToBuffer(SlotConnector __instance, SlotTransferType type, SlotConnectorExtension memoryobj)
        {

            try
            {

                try
                {
                    if (__instance.Owner.Name != null)
                    {
                        memoryobj.NewName = __instance.Owner.Name;
                        if (!memoryobj.Name.Equals(memoryobj.NewName)) { type |= SlotTransferType.Name; }
                    }
                }
                catch
                {
                    //idc
                }
                type |= SlotTransferType.RefID;
                if (type.HasFlag(SlotTransferType.Destroy))
                {
                    type = SlotTransferType.Destroy;
                    type |= SlotTransferType.RefID;
                }

                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(((int)type));




                if (type.HasFlag(SlotTransferType.RefID))
                {
                    
                    MemoryObjectManagement.Save(__instance.Owner.ReferenceID.Position);
                }
                if (type.HasFlag(SlotTransferType.Destroy))
                {
                    MemoryObjectManagement.ReleaseObject();
                    memory.Remove(__instance); //release resources
                    return;
                }




                if (type.HasFlag(SlotTransferType.Name))
                {
                    memoryobj.Name = memoryobj.NewName;

                    MemoryObjectManagement.SaveString(memoryobj.Name);
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
                    MemoryObjectManagement.Save(__instance.Owner.Parent.ReferenceID.Position);
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
