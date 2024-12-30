using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityFrooxEngineRunner;

namespace Thundagun
{

    public class SlotExtension
    {

        public string Name = "";

        public float3 prevposition = new();
        public float3 prevscale = new();
        public floatQ prevrotation = new();
        public bool prevactive = true;

        public ulong IDposition = 0;
        public ulong parentID = 0;

        public Slot instance;

        public bool destroy = false;
        public SlotExtension(Slot __instance)
        {
            this.instance = __instance;
            __instance.Changed += Update;
            Update(__instance);
        }

        public void Update(IChangeable input)
        {
            SlotPatch.WriteDataToBuffer(this.instance, this.instance.IsDestroying || this.destroy, this);
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

    [HarmonyPatch(typeof(FrooxEngine.Slot))]
    public static class SlotPatch
    {
        public static readonly byte TYPE = 1;

        public static Dictionary<Slot, SlotExtension> memory = new();

        public static SlotExtension getmemobj(Slot __instance)
        {
            memory.TryGetValue(__instance, out SlotExtension memoryobj);
            if (memoryobj == null)
            {
                memory.Add(__instance, memoryobj = new SlotExtension(__instance));
            }
            return memoryobj;


        }

        public static List<SlotExtension> EnsureAndGetMemobjParents(Slot __instance)
        {
            List<Slot> parents = new();
            List<SlotExtension> memobjs = new();
            __instance.GetAllParents(parents);
            parents.Reverse();
            foreach (Slot parent in parents)
            {
                memobjs.Add(getmemobj(parent));
            }
            return memobjs;
        }

        public static void DestroyUnusedSlots(Slot __instance)
        {

        }

        public static void DestroySlot(Slot __instance)
        {
            var memobj = getmemobj(__instance);
            memobj.destroy = true;
            
            DestroyUnusedSlots(__instance);
            memobj.Update(__instance);
        }

        public static SlotExtension EnsureMemObj(Slot __instance)
        {
            EnsureAndGetMemobjParents(__instance);
            var memobj = getmemobj(__instance);

            return memobj;
        }
       


        public static void WriteDataToBuffer(Slot __instance, bool destroy, SlotExtension memoryobj)
        {

            try
            {
                //Thundagun.Msg("start" + "1");
                //Thundagun.Msg("start" + "2");
                SlotTransferType type = SlotTransferType.RefID;
                
                if(destroy) type |= SlotTransferType.Destroy;

                //check if fields have changed.
                if(memoryobj.prevactive != __instance.ActiveSelf)
                {
                    type |= SlotTransferType.Active;
                    memoryobj.prevactive = __instance.ActiveSelf;
                }
                if (memoryobj.prevposition != __instance.LocalPosition)
                {
                    type |= SlotTransferType.Position;
                    memoryobj.prevposition = __instance.LocalPosition;
                }
                if (memoryobj.prevrotation != __instance.LocalRotation)
                {
                    type |= SlotTransferType.Rotation;
                    memoryobj.prevrotation = __instance.LocalRotation;
                }
                if (memoryobj.prevscale != __instance.LocalScale)
                {
                    type |= SlotTransferType.Scale;
                    memoryobj.prevscale = __instance.LocalScale;
                }
                if (memoryobj.Name != __instance.Name)
                {
                    type |= SlotTransferType.Name;
                    memoryobj.Name = __instance.Name;
                }
                if(__instance.Parent != null)
                {
                    if (memoryobj.parentID != __instance.Parent.ReferenceID.Position)
                    {
                        type |= SlotTransferType.Parent;
                        memoryobj.parentID = __instance.Parent.ReferenceID.Position;
                    }
                }
                


                //if destroy, only destroy.
                if (type.HasFlag(SlotTransferType.Destroy))
                {
                    type = SlotTransferType.RefID;
                    type |= SlotTransferType.Destroy;
                }
                //Thundagun.Msg("start" + "3");
                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(((int)type));



                //Thundagun.Msg("start" + "4");
                if (type.HasFlag(SlotTransferType.RefID))
                {
                    
                    MemoryObjectManagement.Save(__instance.ReferenceID.Position);
                }
                //Thundagun.Msg("start" + "5");
                if (type.HasFlag(SlotTransferType.Destroy))
                {
                    MemoryObjectManagement.ReleaseObject();
                    memory.Remove(__instance); //release resources
                    memoryobj.instance.Changed -= memoryobj.Update;
                    return;
                }
                //Thundagun.Msg("start" + "6");

                if (type.HasFlag(SlotTransferType.Name))
                {

                    MemoryObjectManagement.SaveString(memoryobj.Name);
                }


                //Thundagun.Msg("start" + "7");

                if (type.HasFlag(SlotTransferType.Active))
                {
                    MemoryObjectManagement.Save(__instance.ActiveSelf);
                }
                //Thundagun.Msg("start" + "8");
                if (type.HasFlag(SlotTransferType.Position))
                {
                    MemoryObjectManagement.Save(__instance.LocalPosition.x);
                    MemoryObjectManagement.Save(__instance.LocalPosition.y);
                    MemoryObjectManagement.Save(__instance.LocalPosition.z);
                }
                //Thundagun.Msg("start" + "9");
                if (type.HasFlag(SlotTransferType.Rotation))
                {
                    MemoryObjectManagement.Save(__instance.LocalRotation.x);
                    MemoryObjectManagement.Save(__instance.LocalRotation.y);
                    MemoryObjectManagement.Save(__instance.LocalRotation.z);
                    MemoryObjectManagement.Save(__instance.LocalRotation.w);
                }
                //Thundagun.Msg("start" + "10");
                if (type.HasFlag(SlotTransferType.Scale))
                {
                    MemoryObjectManagement.Save(__instance.LocalScale.x);
                    MemoryObjectManagement.Save(__instance.LocalScale.y);
                    MemoryObjectManagement.Save(__instance.LocalScale.z);
                }
                //Thundagun.Msg("start" + "11");
                if (type.HasFlag(SlotTransferType.Parent))
                {
                    MemoryObjectManagement.Save(__instance.Parent.ReferenceID.Position);
                }
                //Thundagun.Msg("start" + "12");
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
