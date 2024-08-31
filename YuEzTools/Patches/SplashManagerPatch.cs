using System.Collections;
using System.Threading;
using BepInEx;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;
using YuEzTools.Attributes;
using YuEzTools.Modules;
using YuEzTools.UI;
using YuEzTools.Utils;

namespace YuEzTools.Patches;


[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
public static class SplashManagerPatch
{
    public static bool isLoaded = false;
    public static bool LanguageLoaded = false;
    static SpriteLoader logoSprite = SpriteLoader.FromResource("YuEzTools.Resources.YuET-Logo-tm.png", 300f);
    static SpriteLoader logoGlowSprite = SpriteLoader.FromResource("YuEzTools.Resources.YuET-Logo-tm-mh.png", 300f);
    static TextMeshPro startText = null!;
    static TextMeshPro loadText = null!;
    static TextMeshPro tipText = null!;
    // [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Start)),HarmonyPrefix]
    // public static bool Start_Prefix(SplashManager __instance)
    // {
    //     startText = GameObject.Instantiate(__instance.errorPopup.InfoText,  __instance.logoAnimFinish.transform.FindChild("LogoRoot").FindChild("ISLogo"));
    //     startText.transform.localPosition = new(0, __instance.logoAnimFinish.transform.FindChild("LogoRoot").FindChild("ISLogo").position.y -1.18f, 0);
    //     startText.fontStyle = TMPro.FontStyles.Bold;
    //     startText.text = "欢迎使用YuET!\n<size=65%>Welcome use YuET!</size>";
    //     startText.color = Color.white.AlphaMultiplied(0.3f);
    //     startText.SetActive(__instance.logoAnimFinish.enabled);
    //     return true;
    // }
    private static bool IsLoadingAll = false;

    static IEnumerator CoLoadYuET(SplashManager __instance)
    {
        
        var logo = UnityHelper.CreateObject<SpriteRenderer>("YuETLogo", null, new Vector3(0, 0.2f, -5f));
        var logoGlow = UnityHelper.CreateObject<SpriteRenderer>("YuETLogoGlow", null, new Vector3(0, 0.2f, -5f));
        logo.sprite = logoSprite.GetSprite();
        logoGlow.sprite = logoGlowSprite.GetSprite();
        
        logoGlow.color = Color.white;
        logoGlow.transform.localScale = Vector3.one;
        logo.color = Color.white;
        logoGlow.gameObject.SetActive(false);
        logo.transform.localScale = Vector3.one;
        

        loadText = GameObject.Instantiate(__instance.errorPopup.InfoText, null);
        loadText.transform.localPosition = new(0f, -2f, -10f);
        loadText.fontStyle = FontStyles.Bold;
        loadText.text = "加载中...\nLoading...";
        loadText.color = Color.white.AlphaMultiplied(0.3f);
        
        tipText = GameObject.Instantiate(__instance.errorPopup.InfoText, null);
        tipText.transform.localPosition = new(0f, -4f, -10f);
        tipText.fontStyle = FontStyles.Bold;
        tipText.text = "加载中...\nLoading...";
        tipText.color = Color.white.AlphaMultiplied(0.3f);
        
        float p = 1f;
        while (p > 0f)
        {
            p -= Time.deltaTime * 2.8f;
            float alpha = 1 - p;
            logo.color = Color.white.AlphaMultiplied(alpha);
            logoGlow.color = Color.white.AlphaMultiplied(alpha);
            logo.transform.localScale = Vector3.one * (p * p * 0.012f + 1f);
            logoGlow.transform.localScale = Vector3.one * (p * p * 0.012f + 1f);
            yield return null;
        }
        
        #region Loading
        Logger.Info("Loading...","Load");
        loadText.text = "正在存放必要的文件\n<size=65%>The necessary documents are being stored</size>";
        ResourceUtils.WriteToFileFromResource(
            "BepInEx/core/YamlDotNet.dll",
            "YuEzTools.Resources.InDLL.Depends.YamlDotNet.dll");
        ResourceUtils.WriteToFileFromResource(
            "BepInEx/core/YamlDotNet.xml",
            "YuEzTools.Resources.InDLL.Depends.YamlDotNet.xml");
        
        loadText.text = "加载多语言\n<size=65%>Loading Language</size>";
        PluginModuleInitializerAttribute.InitializeAll();
        LanguageLoaded = true;

        loadText.text = "准备下载封禁名单\n<size=65%>Wait for Downloading Banlist</size>";
        CloudBanlistLoader.DownloadBanlist();
        yield return new WaitForSeconds(0.03f);
        while (CloudBanlistLoader.isLoading)
        {
            if (CloudBanlistLoader.isBrocked) yield break;
            // Logger.Info("下载Banlist","CoLoadYuET");
            loadText.text = "下载封禁名单\n<size=65%>Downloading Banlist</size>";
            if (CloudBanlistLoader.TotalFileSize != null && CloudBanlistLoader.TotalBytesDownloaded != null) 
            {
                tipText.text =
                    $"{CloudBanlistLoader.TotalFileSize / 1000}KB / {CloudBanlistLoader.TotalBytesDownloaded / 1000}KB  -  {(int)CloudBanlistLoader.ProgressPercentage}%";
                
            }
            yield return false;
        }
        tipText.text = "加载中...\nLoading...";

        loadText.text = "初始化随机数\n<size=65%>Loading Random</size>";
        IRandom.SetInstance(new NetRandomWrapper());

        loadText.text = "加载配置\n<size=65%>Loading Config</size>";
        Toggles.WinTextSize = Main.WinTextSize.Value;
        
        //Translator.Init();
        
        loadText.text = "检查AmongUs版本\n<size=65%>Check AmongUs Version</size>";
        if (Application.version == Main.CanUseInAmongUsVer)
            Logger.Info($"AmongUs Version: {Application.version}","AmongUsVersionCheck"); //牢底居然有智齿的版本？！
        else
            Logger.Info($"游戏本体版本过低或过高,AmongUs Version: {Application.version}","AmongUsVersionCheck"); //牢底你的版本也不行啊
        
        loadText.text = "启用/禁用控制台\n<size=65%>Enable Console or Disable</size>";
        if (Main.ModMode != 0) ConsoleManager.DetachConsole();
        else ConsoleManager.CreateConsole();
        
        loadText.text = "加载开发组名单\n<size=65%>Loading Devs List</size>";
        DevManager.Init();
        
        //模组加载好了标语
        loadText.text = "完成...\n<size=65%>Finished</size>";
        Logger.Msg("========= YuET loaded! =========", "YuET Plugin Load");
        IsLoadingAll = true;
        #endregion
        
        tipText.text = "加载完成...\n<size=65%>Loaded</size>";
        loadText.text = "完成...\n<size=65%>Finished</size>";
        logo.gameObject.SetActive(false);
        logoGlow.gameObject.SetActive(true);

        // for (int i = 0; i < 10; i++)
        // {
        //     // logo.gameObject.SetActive(true);
        //     // logoGlow.gameObject.SetActive(false);
        //     loadText.gameObject.SetActive(true);
        //     tipText.gameObject.SetActive(true);
        //     yield return new WaitForSeconds(0.03f);
        //     // logo.gameObject.SetActive(false);
        //     // logoGlow.gameObject.SetActive(false);
        //     loadText.gameObject.SetActive(false);
        //     tipText.gameObject.SetActive(false);
        //     yield return new WaitForSeconds(0.03f);
        // }
        logo.gameObject.SetActive(false);
        logoGlow.gameObject.SetActive(true);
        // GameObject.Destroy(loadText.gameObject);
        // GameObject.Destroy(tipText.gameObject);
        p = 1f;
        while (p > 0f)
        {
            p -= Time.deltaTime * 1.2f;
            logo.color = Color.white.AlphaMultiplied(p);
            logoGlow.color = Color.white.AlphaMultiplied(p);
            loadText.color = Color.white.AlphaMultiplied(p);
            tipText.color = Color.white.AlphaMultiplied(p);
            yield return null;
        }
        logo.color = Color.clear;
        logoGlow.color = Color.clear;
        loadText.color = Color.clear;
        tipText.color = Color.clear;
        __instance.sceneChanger.AllowFinishLoadingScene();
        __instance.startedSceneLoad = true;
    }

    private static bool first = true;
    public static bool Prefix(SplashManager __instance)
    {
        __instance.logoAnimFinish.transform.FindChild("LogoRoot").FindChild("ISLogo").GetComponent<SpriteRenderer>().sprite = logoSprite.GetSprite();

        if(first)
        {
            startText = GameObject.Instantiate(__instance.errorPopup.InfoText,
                __instance.logoAnimFinish.transform.FindChild("LogoRoot").FindChild("ISLogo"));
            startText.transform.localPosition = new(0,
                __instance.logoAnimFinish.transform.FindChild("LogoRoot").FindChild("ISLogo").position.y - 1.18f, 0);
            startText.fontStyle = TMPro.FontStyles.Bold;
            startText.text = "欢迎使用YuET!\n<size=65%>Welcome to use YuET!</size>";
            startText.color = Color.white.AlphaMultiplied(0.3f);
            startText.SetActive(__instance.logoAnimFinish.enabled);
            first = false;
        }
        
        if (__instance.doneLoadingRefdata && !__instance.startedSceneLoad && Time.time - __instance.startTime > __instance.minimumSecondsBeforeSceneChange && !isLoaded)
        {
            isLoaded = true;
            __instance.StartCoroutine(CoLoadYuET(__instance).WrapToIl2Cpp());
        }
        return false;
    }
}
