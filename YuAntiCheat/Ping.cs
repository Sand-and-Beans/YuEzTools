// using HarmonyLib;
// using UnityEngine;
// using UnityEngine.UI;
// using System.Diagnostics;
// using TMPro;
// using YuAntiCheat;
// using YuAntiCheat.Get;
// using YuAntiCheat.Utils;
//
// namespace YuAntiCheat;
//
// [HarmonyPriority(Priority.Low)]
// [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
// public static class PingTracker_Update
// {
//     [HarmonyPostfix]
//     public static void Postfix(PingTracker __instance)
//     {
//         var offset_x = 2.5f; //右端からのオフセット
//         var offset_y = 6.1f; //右端からのオフセット
//         if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.gameObject.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
//         if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
//         __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, offset_y, 0f);
//
//         __instance.text.text = __instance.ToString();
//         __instance.text.alignment = TextAlignmentOptions.TopRight;
//         __instance.text.text =
//             $"<color={Main.ModColor}>{Main.ModName}</color><color=#00FFFF> v{Main.PluginVersion}</color>\n{Main.MainMenuText}";
//
//         if (Main.safemode)
//              __instance.text.text += "\n<color=#DC143C>[Safe]</color>";
//         else
//             __instance.text.text += "\n<color=#1E90FF>[UnSafe]</color>";
//         
//         // if (Main.ShowMode) //煞笔AU chat as会kick AC端
//         //     __instance.text.text += "\n<color=#00BFFF>[Show Mode]</color>";
//         // else
//         //     __instance.text.text += "\n<color=#7FFFAA>[UnShow Mode]</color>";
//
//      __instance.text.text += "\n<color=#FFFF00>By</color> <color=#FF0000>Yu</color>";
// #if DEBUG
// __instance.text.text += "\n<color=#FFC0CB>Debug</color>";
// #endif
// #if CANARY
//         __instance.text.text += "\n<color=#6A5ACD>Canary</color>";
// #endif
//         __instance.text.text += Utils.Utils.getColoredPingText(AmongUsClient.Instance.Ping); // Colored Ping
//     }
// }