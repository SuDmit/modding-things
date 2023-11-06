using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PineappleHeatPump
{
	public static class HeatPumpInteractions
	{
		public static ThingDef HeatPump_Over = null;
		public static ThingDef HeatPump_Over2W = null;
		
		//extends hardcoded check to allow placement over wall
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
			Harmony harmony = new Harmony("SuDmit.Rimworld.HeatPumpOver.main");
		
#if DEBUG		
			Log.Message("[HeatPumpOver - Manual Patch] Starting initialization");
			FileLog.Log("[HeatPumpOver - Manual Patch] Starting initialization");
#endif			
			//searches for specific class among all loaded
			Type classInstance = (
				from asm in AppDomain.CurrentDomain.GetAssemblies()
				from type in asm.GetTypes()
				where type.IsClass && type.FullName == "Replace_Stuff.OverWallDef"
				select type).SingleOrDefault(); //.SingleOrDefault() instead of .Single() prevents exception if not found
			
			
			if (classInstance == null)
			{
#if DEBUG
				Log.Message("[HeatPumpOver - Manual Patch] [ReplaceStuff] Mod not present");
				FileLog.Log("[HeatPumpOver - Manual Patch] [ReplaceStuff] Mod not present");
#endif
				return;
			}
			
#if DEBUG	//Not sure if try-catch block is really necessary, because if class exists, everything should work fine, and if not we already returned. Also if errors occur, them probably better be red I guess
			try 
            {	
#endif			
				HeatPumpInteractions.HeatPump_Over = DefDatabase<ThingDef>.GetNamed("HeatPump_Over");
				HeatPumpInteractions.HeatPump_Over2W = DefDatabase<ThingDef>.GetNamed("HeatPump_Over2W");
				
				//hooks up to Replace_Stuff.OverWallDef.IsOverWall() check
				harmony.Patch(AccessTools.Method(classInstance, "IsOverWall"), null, postfix: new HarmonyMethod(typeof(HeatPumpInteractions), nameof(HeatPumpInteractions.IsOverWall)));
				
				//similar, but to PlaceWorker_ReversibleHeatPump.DrawGhost()
				harmony.Patch(typeof(PlaceWorker_ReversibleHeatPump).GetMethod("DrawGhost"), null, null, transpiler: new HarmonyMethod(typeof(WideVentLocationGhost), nameof(WideVentLocationGhost.Transpiler)));
	
#if DEBUG				
				Log.Message("[HeatPumpOver - Manual Patch] Successful");
				FileLog.Log("[HeatPumpOver - Manual Patch] Successful"); 
						
			}
			catch (Exception ex)
			{

				Log.Message("[HeatPumpOver - Manual Patch] Error when init manual patching method");
				FileLog.Log("[HeatPumpOver - Manual Patch] Error when init manual patching method");
					
                Log.Message(ex.ToString());
                FileLog.Log(ex.ToString());
			}
#endif			
        }
    }
 
	//@SuDmit: Straight up copypasted entire class from Replace_Stuff source section for vanilla PlaceWorker_Cooler without real understanding. Does some magic with positioning of exhaust square
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
