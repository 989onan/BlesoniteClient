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
using SharpDX;

#endregion

namespace Thundagun;

public class Thundagun : ResoniteMod
{
    public const string AuthorString = "Fro Zen, 989onan, DoubleStyx, Nytra, Merith-TK, SectOLT"; // in order of first commit
    public const string VersionString = "1.2.0"; // change minor version for config "API" changes

    public static MemoryMappedFile MemoryFrooxEngine;

    public override string Name => "BlesoniteClient";
    public override string Author => AuthorString;
    public override string Version => VersionString;
    public override string Link => "https://github.com/Frozenreflex/Thundagun";


    public override void OnEngineInit()
    {
        var harmony = new Harmony("com.989onan.BlesoniteClient");




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



