using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PineappleHeatPump
{
	public static class HeatPumpInteractions
	{
		public static ThingDef HeatPump_Over = null;
		public static ThingDef HeatPump_Over2W = null;
		
		public static void IsOverWall(this BuildableDef bdef, ref bool __result)
		{
			if (!__result)
				__result = bdef == HeatPump_Over || bdef == HeatPump_Over2W ;
		}
	}
	
	
    [StaticConstructorOnStartup]
    public static class Mod
    {			
        static Mod()
        {		
#if DEBUG
//			Harmony.DEBUG = true;
			FileLog.Reset();
			FileLog.Log("DEBUG");
#endif
			Harmony harmony = new Harmony("SuDmit.Rimworld.HeatPumpOver.main");
#if DEBUG			
			Log.Message("Hello World1!");
			FileLog.Log("Hello World1!");
#endif		
			
#if DEBUG		
			Log.Message("[HeatPumpOver - Manual Patch] Starting initialization");
			FileLog.Log("[HeatPumpOver - Manual Patch] Starting initialization");
#endif				
			Type classInstance = (
				from asm in AppDomain.CurrentDomain.GetAssemblies()
				from type in asm.GetTypes()
				where type.IsClass && type.FullName == "Replace_Stuff.OverWallDef"
				select type).SingleOrDefault();

			if (classInstance == null)
			{
#if DEBUG
				Log.Message("[HeatPumpOver - Manual Patch] [ReplaceStuff] Mod not present");
				FileLog.Log("[HeatPumpOver - Manual Patch] [ReplaceStuff] Mod not present");
#endif
				return;
			}
						
/*			ThingDef myDef1 = DefDatabase<ThingDef>.GetNamed("HeatPump_Over");
			if (myDef1 != null)
			{
				HeatPumpInteractions.HeatPump_Over = myDef1;
				Log.Message(myDef1.ToString());
				FileLog.Log(myDef1.ToString()); 
			}
*/			
/*			ThingDef myDef2 = DefDatabase<ThingDef>.GetNamed("HeatPump_Over2W");
			if (myDef2 != null)
			{
				HeatPumpInteractions.HeatPump_Over2W = myDef2;	
				Log.Message(myDef2.ToString());
				FileLog.Log(myDef2.ToString()); 
			}						
*/			

			try
            {	
				harmony.Patch(AccessTools.Method(classInstance, "IsOverWall"), null, postfix: new HarmonyMethod(typeof(HeatPumpInteractions), nameof(HeatPumpInteractions.IsOverWall)));
							
				harmony.Patch(typeof(PlaceWorker_ReversibleHeatPump).GetMethod("DrawGhost"), null, null, transpiler: new HarmonyMethod(typeof(WideVentLocationGhost), nameof(WideVentLocationGhost.Transpiler)));
				
				HeatPumpInteractions.HeatPump_Over = DefDatabase<ThingDef>.GetNamed("HeatPump_Over");
				HeatPumpInteractions.HeatPump_Over2W = DefDatabase<ThingDef>.GetNamed("HeatPump_Over2W");
				
#if DEBUG				
					Log.Message("[HeatPumpOver - Manual Patch] Successful");
					FileLog.Log("[HeatPumpOver - Manual Patch] Successful"); 
#endif					
			}
			catch (Exception ex)
			{
#if DEBUG
				Log.Message("[HeatPumpOver - Manual Patch] Error when init manual patching method");
				FileLog.Log("[HeatPumpOver - Manual Patch] Error when init manual patching method");
				
                Log.Message(ex.ToString());
                FileLog.Log(ex.ToString());
#endif				
			}
			
        }
    }
 
	//Straight up copypasted from Replace_Stuff section for vanilla PlaceWorker_Cooler
	static class WideVentLocationGhost
	{
		public static IEnumerable<CodeInstruction> TranspileNorthWith(IEnumerable<CodeInstruction> instructions, OpCode paramCode)
		{
			FieldInfo NorthInfo = AccessTools.Field(typeof(IntVec3), nameof(IntVec3.North));

			MethodInfo DoubleItInfo = AccessTools.Method(typeof(WideVentLocationGhost), nameof(WideVentLocationGhost.DoubleIt));

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				//IL_0019: call         valuetype Verse.IntVec3 Verse.IntVec3::get_North()
				if (i.LoadsField(NorthInfo))
				{
					yield return new CodeInstruction(paramCode);//def or thing
					yield return new CodeInstruction(OpCodes.Call, DoubleItInfo);
				}
			}
		}
		
		//public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return TranspileNorthWith(instructions, OpCodes.Ldarg_1);
		}

		public static IntVec3 DoubleIt(IntVec3 v, object o)
		{
			ThingDef thingDef = o as ThingDef ?? (o as Thing)?.def;
			IntVec3 result = thingDef == HeatPumpInteractions.HeatPump_Over2W ||
				thingDef.entityDefToBuild == HeatPumpInteractions.HeatPump_Over2W ? v * 2 : v;
			return result;
		}
	}
}
