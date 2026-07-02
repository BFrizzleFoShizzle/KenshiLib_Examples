using FCS_extended;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace Dialogue_FCS
{	
	enum DialogConditionEnum_extended
	{
		DC_IS_SLEEPING = 1000,
		DC_HAS_SHORT_TERM_TAG,
		DC_IS_ALLY_BECAUSE_OF_DISGUISE,
		DC_STAT_LEVEL_UNMODIFIED,
		DC_STAT_LEVEL_MODIFIED,
		DC_WEAPON_LEVEL,
		DC_ARMOUR_LEVEL
	}

	enum itemType_extended
	{
		// 1000 is used by WorldStatesPlugin
		ITEM_ANY = 1001,
		CHARACTER_ANY
	}


	public class DialoguePlugin : IPlugin
	{
		public int Init(Assembly assembly)
		{
			Harmony harmony = new Harmony("Dialogue_FCS");
			harmony.PatchAll();

			Console.WriteLine("Dialogue plugin loaded.");
			return 0;
		}

		// patch to enable the condition tag box/add default condition tag value
		[HarmonyPatch("forgotten_construction_set.dialog.ConditionControl", "createDefaults")]
		public static class ConditionControl_createDefaults_Patch
		{
			[HarmonyPostfix]
			static void Postfix()
			{

				object conditionDefaults = Traverse.Create(AccessTools.TypeByName("forgotten_construction_set.dialog.ConditionControl")).Field("conditionDefaults").GetValue();
				
				Type condDefType = typeof(Dictionary<,>).MakeGenericType(new Type[] { AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"), typeof(object) });
				MethodInfo method = condDefType.Method("Add", new Type[] { AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"), typeof(object) });
				// add CharacterPerceptionTags_ShortTerm tag to DC_HAS_SHORT_TERM_TAG
				condDefType.Method("Add", new Type[] { AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"), typeof(object) })
					.Invoke(conditionDefaults, new object[]{
					Enum.ToObject(AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"),(int)DialogConditionEnum_extended.DC_HAS_SHORT_TERM_TAG),
					(AccessTools.TypeByName("forgotten_construction_set.CharacterPerceptionTags_ShortTerm").GetMember("ST_NONE").First() as FieldInfo).GetValue(null) });
				// add StatsEnumerated tag to DC_STAT_LEVEL_UNMODIFIED
				condDefType.Method("Add", new Type[] { AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"), typeof(object) })
					.Invoke(conditionDefaults, new object[]{
					Enum.ToObject(AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"),(int)DialogConditionEnum_extended.DC_STAT_LEVEL_UNMODIFIED),
					(AccessTools.TypeByName("forgotten_construction_set.StatsEnumerated").GetMember("STAT_NONE").First() as FieldInfo).GetValue(null) });
				// add StatsEnumerated tag to DC_STAT_LEVEL_MODIFIED
				condDefType.Method("Add", new Type[] { AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"), typeof(object) })
					.Invoke(conditionDefaults, new object[]{
					Enum.ToObject(AccessTools.TypeByName("forgotten_construction_set.DialogConditionEnum"),(int)DialogConditionEnum_extended.DC_STAT_LEVEL_MODIFIED),
					(AccessTools.TypeByName("forgotten_construction_set.StatsEnumerated").GetMember("STAT_NONE").First() as FieldInfo).GetValue(null) });
			}
		}

		// patch to add ITEM_ANY type that allows refferences to any ITEM sub-type
		[HarmonyPatch("forgotten_construction_set.ItemFilter", "Test")]
		public static class ItemFilter_Test_Patch
		{
			public static int NULL_ITEM = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("NULL_ITEM").First() as FieldInfo).GetValue(null);

			public static int ITEM = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("ITEM").First() as FieldInfo).GetValue(null);
			public static int WEAPON = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("WEAPON").First() as FieldInfo).GetValue(null);
			public static int ARMOUR = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("ARMOUR").First() as FieldInfo).GetValue(null);
			public static int CROSSBOW = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("CROSSBOW").First() as FieldInfo).GetValue(null);
			public static int CONTAINER = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("CONTAINER").First() as FieldInfo).GetValue(null);
			public static int NEST_ITEM = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("NEST_ITEM").First() as FieldInfo).GetValue(null);
			public static int MAP_ITEM = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("MAP_ITEM").First() as FieldInfo).GetValue(null);
			public static int LIMB_REPLACEMENT = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("LIMB_REPLACEMENT").First() as FieldInfo).GetValue(null);

			// Note: I don't think HUMAN_CHARACTER is used?
            public static int HUMAN_CHARACTER = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("HUMAN_CHARACTER").First() as FieldInfo).GetValue(null);
            public static int ANIMAL_CHARACTER = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("ANIMAL_CHARACTER").First() as FieldInfo).GetValue(null);
            public static int CHARACTER = (int)(AccessTools.TypeByName("forgotten_construction_set.itemType").GetMember("CHARACTER").First() as FieldInfo).GetValue(null);

            [HarmonyPrefix]
			static void Prefix(object __instance, object item, out int __state)
			{
				// backup type filter
				__state = (int)Traverse.Create(__instance).Field("type").GetValue();
				// clear type filter
				if (__state == (int)itemType_extended.ITEM_ANY || __state == (int)itemType_extended.CHARACTER_ANY)
				{
					Traverse.Create(__instance).Field("type").SetValue(NULL_ITEM);
				}
				else
				{
					// optimization - disable the postfix to make the filter faster
					__state = NULL_ITEM;
				}
			}
			[HarmonyPostfix]
			static void Postfix(ref bool __result, object __instance, object item, int __state)
			{
				if (__state == (int)itemType_extended.ITEM_ANY)
				{
					// load backup type filter
					Traverse.Create(__instance).Field("type").SetValue(__state);
					// we need to do type filtering ourselves
					int type = (int)Traverse.Create(item).Property("type").GetValue();
					__result = __result && (type == ITEM || type == WEAPON || type == ARMOUR || type == CROSSBOW
						|| type == CONTAINER || type == NEST_ITEM || type == MAP_ITEM || type == LIMB_REPLACEMENT);
                }
                else if (__state == (int)itemType_extended.CHARACTER_ANY)
                {
                    // load backup type filter
                    Traverse.Create(__instance).Field("type").SetValue(__state);
                    // we need to do type filtering ourselves
                    int type = (int)Traverse.Create(item).Property("type").GetValue();
                    __result = __result && (type == CHARACTER || type == HUMAN_CHARACTER || type == ANIMAL_CHARACTER);
                }
            }
		}
		// patch to suppress incorrect refference type errors for "ITEM_ANY"
		[HarmonyPatch("forgotten_construction_set.ErrorWindow", "addError")]
		public static class ErrorWindow_addError_Patch
		{
			// forgotten_construction_set.ErrorCode.ReferenceToIncorrectType
			public static int ReferenceToIncorrectType = (int)(AccessTools.TypeByName("forgotten_construction_set.ErrorCode").GetMember("ReferenceToIncorrectType").First() as FieldInfo).GetValue(null);

			[HarmonyPostfix]
			static bool Prefix(object type, object item, string mod, string[] textArgs)
			{
				if ((int)type == ReferenceToIncorrectType)
				{
					if (textArgs != null && textArgs.Length >= 4)
					{
						if (textArgs[2] == "ITEM_ANY")
						{
							if(textArgs[3] == "ITEM" || textArgs[3] == "WEAPON" || textArgs[3] == "ARMOUR"
								|| textArgs[3] == "CROSSBOW" || textArgs[3] == "CONTAINER" || textArgs[3] == "NEST_ITEM"
								|| textArgs[3] == "MAP_ITEM" || textArgs[3] == "LIMB_REPLACEMENT")
							{
								// drop call and suppress error
								return false;
							}
                        }
                        else if (textArgs[2] == "CHARACTER_ANY")
                        {
                            if (textArgs[3] == "CHARACTER" || textArgs[3] == "HUMAN_CHARACTER" || textArgs[3] == "ANIMAL_CHARACTER")
                            {
                                // drop call and suppress error
                                return false;
                            }
                        }
                    }
				}
				// default case - run original implementation
				return true;
			}
		}
	}
}
