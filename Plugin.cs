using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MoonlighterBestDeal {
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin {
		internal static ManualLogSource Log;

		private void Awake() {
			Plugin.Log = base.Logger;

			var harmony = new Harmony("sh.arnaud.moonlighterbestdeal");

			harmony.PatchAll();
		}
	}
}

namespace MoonlighterBestDeal.Patches {
	[HarmonyPatch(typeof(ShowcaseSlotGUI), nameof(ShowcaseSlotGUI.ResetEditingPrize))]
	class ResetEditingPrizePatch {
		// TODO: Check if there's a better way to gennerate the MethodInfo.

		static MethodInfo m_getlastprice = SymbolExtensions.GetMethodInfo(
				() => ItemPriceManager.Instance.GetLastPrice(null, ItemPriceValoration.Cheap)
				);

		static MethodInfo m_getmaxcorrectprice = SymbolExtensions.GetMethodInfo(
				() => ItemPriceManager.Instance.GetMaxCorrectPrice(null, false, false)
				);

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var found = false;

			foreach (var instruction in instructions) {
				if (instruction.Calls(m_getlastprice)) {
					// Removes the price valoration argument from the stack.
					yield return new CodeInstruction(OpCodes.Pop);

					// Push the two optional arguments onto the stack
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);

					// Call the GetMaxCorrectPrice function instead.
					yield return new CodeInstruction(OpCodes.Call, m_getmaxcorrectprice);

					found = true;
				} else {
					yield return instruction;
				}
			}

			if (!found) {
				MoonlighterBestDeal.Plugin.Log.LogError($"Call to ItemPriceManager.GetLastPrice not found in ShowcaseSlotGUI.ResetEditingPrize");
			}
		}
	}
}
