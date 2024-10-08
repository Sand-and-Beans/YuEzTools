using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
using AmongUs.Data;
using Assets.InnerNet;
using System.Linq;
using System.Net;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System.IO;
using System.Reflection;

namespace YuEzTools;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public class MMCheck
{
    private static void Prefix(MainMenuManager __instance)
    {
        // 获取所有已加载的插件
        var loadedPlugins = IL2CPPChainloader.Instance.Plugins.Values;
        // 检查是否有目标插件
        var targetPlugin = loadedPlugins.FirstOrDefault(plugin => plugin.Metadata.Name == "MalumMenu" || plugin.Metadata.Name == "MalumMenu-Yu" || plugin.Metadata.Name == "MalumMenuYu");
        

        if (targetPlugin != null)
        {
            Main.Logger.LogMessage("找到了MalumMenu系列捏~关闭咯！");
            foreach (var path in Directory.EnumerateFiles(
                         Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.*"))
            {
                if (path.EndsWith("MalumMenu.dll") || path.EndsWith("MalumMenu-Yu.dll"))
                {
                    Harmony.UnpatchAll();//当进入MainMenu时检测加载如果有MM 就自动关闭
                    Main.Logger.LogInfo($"{Path.GetFileName(path)} 已删除");
                    File.Delete(path);
                }
            }
        }
    }
    
}