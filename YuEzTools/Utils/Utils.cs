using UnityEngine;
using InnerNet;
using System.Linq;
using Il2CppSystem.Collections.Generic;
using System.IO;
using Hazel;
using System.Reflection;
using AmongUs.GameOptions;
using Sentry.Internal.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using static YuEzTools.Translator;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using YuEzTools.Get;
using YuEzTools.Modules;
using YuEzTools.Patches;

namespace YuEzTools.Utils;

public static class Utils
{
    public static Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();
    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadSprite");
        }
        return null;
    }
    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            ImageConversion.LoadImage(texture, ms.ToArray(), false);
            return texture;
        }
        catch
        {
            Logger.Error($"Failed to read Texture： {path}", "LoadTextureFromResources");
        }
        return null;
    }
    
    public static string GetDeadText(PlayerControl pc)
    {
        string color = "#ffffff";
        string text = "";
        string alltext = "";
        switch (pc.GetPlayerData().DeadReason)
        {
            case DeadReasonData.Kill:
                text = string.Format(GetString("ByKilled"),pc.GetPlayerData().Killer.Name);
                color = "#FF4949";
                break;
            case DeadReasonData.Exile:
                text = GetString("Exile");
                color = "#49FF85";
                break;
            case DeadReasonData.Disconnect:
                text = GetString("Disconnect");
                color = "#888888";
                break;
            case DeadReasonData.Alive:
                text = GetString("Alive");
                color = "#F3FF49";
                break;
        }

        alltext = $"<color={color}>{text}</color>";
        return alltext;
    }

    public static string GetKillOrTaskCountText(this byte id)
    {
        var thisdata = id.GetPlayerDataById();
        var text = "[ERROR]";
        var color = Color.white;
        
        if (thisdata.IsImpostor)
        {
            text = GetString("KillCount") + thisdata.KillCount.ToString();
            color = Color.red;
        }
        else
        {
            text = $"{thisdata.TaskCount}/{PlayerControlSetTasksPatch.TaskCount}";
            color = thisdata.TaskCount == PlayerControlSetTasksPatch.TaskCount ? Color.green : (thisdata.TaskCount != 0 ? Color.yellow : Color.gray);
        }

        text = ColorString(color, text);
        
        return text;
    }
    //感谢FSX
    public static string SummaryTexts(byte id)
    {

        var thisdata = ModPlayerData.GetModPlayerDataById(id);

        var builder = new StringBuilder();
        var longestNameByteCount = ModPlayerData.GetLongestNameByteCount();


        var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
        
        builder.Append(ColorString(thisdata.Color, thisdata.Name));
        
        builder.AppendFormat("<pos={0}em>", pos).Append(GetKillOrTaskCountText(id)).Append("</pos>");
        pos += 8f;

        builder.AppendFormat("<pos={0}em>", pos).Append(GetDeadText(thisdata.pc)).Append("</pos>");
        pos += DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 14f : 10.5f;

        builder.AppendFormat("<pos={0}em>", pos);

        var oldrole = thisdata.role;
        var newrole = thisdata.RoleAfterDeath ?? (thisdata.pc.Data.Role.IsImpostor ? RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost);
        builder.Append(ColorString(GetRoleColor32(oldrole), GetString($"{oldrole}")));

        if (thisdata.IsDead && newrole != oldrole)
        {
            builder.Append($" => {ColorString(GetRoleColor32(newrole), GetRoleString($"{newrole}"))}");
        }
        builder.Append("</pos>");

        return builder.ToString();
    }
    public static bool HasTasks(this PlayerControl p)
    {
        if (!p.IsImpostor()) return true;
        return false;
    }
    
    public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "<Default>", bool removeTags = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (title == "<Default>") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
        Main.isChatCommand = true;
        Main.MessagesToSend.Add((removeTags ? text.RemoveHtmlTags() : text, sendTo, title + '\0'));
    }
    
    public static Color ShadeColor(this Color color, float Darkness = 0)
    {
        bool IsDarker = Darkness >= 0; //黒と混ぜる
        if (!IsDarker) Darkness = -Darkness;
        float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
        float R = (color.r + Weight) / (Darkness + 1);
        float G = (color.g + Weight) / (Darkness + 1);
        float B = (color.b + Weight) / (Darkness + 1);
        return new Color(R, G, B, color.a);
    }

    public static Color GetRoleColor(RoleTypes rt)
    {
        Color c = new Color();
        switch (rt)
        {
            /*=== 船员 === */
            case RoleTypes.Crewmate:
                c = new Color(30,144,255); // 船员 => 道奇蓝
                break;
            
            case RoleTypes.Noisemaker:
                c = new Color(0,191,255); // 大嗓门 => 深天蓝
                break;
            
            case RoleTypes.Scientist:
                c = new Color(0,255,255); // 科学家 => 青色
                break;
            
            case RoleTypes.Engineer:
                c = new Color(127,255,170); // 工程师 => 绿玉
                break;
            
            case RoleTypes.Tracker:
                c = new Color(0,128,128); // 追踪 => 水鸭色
                break;
            
            /*=== 内鬼 === */
            case RoleTypes.Impostor:
                c = new Color(255,0,0); // 内鬼 => 纯红
                break;
            
            case RoleTypes.Shapeshifter:
                c = new Color(255,69,0); // 变形 => 橙红
                break;
            
            case RoleTypes.Phantom:
                c = new Color(250,128,114); // 隐身 => 鲜肉
                break;
            
            /*=== 灵魂 === */
            case RoleTypes.CrewmateGhost:
                c = new Color(220,220,220); // 船员灵魂 => 亮灰色
                break;
            
            case RoleTypes.GuardianAngel:
                c = new Color(240,128,128); // 天使 => 淡珊瑚
                break;
            
            case RoleTypes.ImpostorGhost:
                c = new Color(255,228,225); // 内鬼灵魂 => 薄雾玫瑰
                break;
            
        }

        return c;
    }
    public static Color32 GetRoleColor32(RoleTypes rt)
    {
        Color32 c = new Color32();
        switch (rt)
        {
            /*=== 船员 === */
            case RoleTypes.Crewmate:
                c = new Color32(30,144,255,byte.MaxValue); // 船员 => 道奇蓝
                break;
            
            case RoleTypes.Noisemaker:
                c = new Color32(0,191,255,byte.MaxValue); // 大嗓门 => 深天蓝
                break;
            
            case RoleTypes.Scientist:
                c = new Color32(0,255,255,byte.MaxValue); // 科学家 => 青色
                break;
            
            case RoleTypes.Engineer:
                c = new Color32(127,255,170,byte.MaxValue); // 工程师 => 绿玉
                break;
            
            case RoleTypes.Tracker:
                c = new Color32(0,128,128,byte.MaxValue); // 追踪 => 水鸭色
                break;
            
            /*=== 内鬼 === */
            case RoleTypes.Impostor:
                c = new Color32(255,0,0,byte.MaxValue); // 内鬼 => 纯红
                break;
            
            case RoleTypes.Shapeshifter:
                c = new Color32(255,69,0,byte.MaxValue); // 变形 => 橙红
                break;
            
            case RoleTypes.Phantom:
                c = new Color32(250,128,114,byte.MaxValue); // 隐身 => 鲜肉
                break;
            
            /*=== 灵魂 === */
            case RoleTypes.CrewmateGhost:
                c = new Color32(220,220,220,byte.MaxValue); // 船员灵魂 => 亮灰色
                break;
            
            case RoleTypes.GuardianAngel:
                c = new Color32(240,128,128,byte.MaxValue); // 天使 => 淡珊瑚
                break;
            
            case RoleTypes.ImpostorGhost:
                c = new Color32(255,228,225,byte.MaxValue); // 内鬼灵魂 => 薄雾玫瑰
                break;
            
        }

        return c;
    }
    public static string GetRoleHtmlColor(RoleTypes rt)
    {
        string c = "";
        switch (rt)
        {
            /*=== 船员 === */
            case RoleTypes.Crewmate:
                c = "#1E90FF"; // 船员 => 道奇蓝
                break;
            
            case RoleTypes.Noisemaker:
                c = "#00BFFF"; // 大嗓门 => 深天蓝
                break;
            
            case RoleTypes.Scientist:
                c = "#00FFFF"; // 科学家 => 青色
                break;
            
            case RoleTypes.Engineer:
                c = "#7FFFAA"; // 工程师 => 绿玉
                break;
            
            case RoleTypes.Tracker:
                c = "#008080"; // 追踪 => 水鸭色
                break;
            
            /*=== 内鬼 === */
            case RoleTypes.Impostor:
                c = "#FF0000"; // 内鬼 => 纯红
                break;
            
            case RoleTypes.Shapeshifter:
                c = "#FF4500"; // 变形 => 橙红
                break;
            
            case RoleTypes.Phantom:
                c = "#FA8072"; // 隐身 => 鲜肉
                break;
            
            /*=== 灵魂 === */
            case RoleTypes.CrewmateGhost:
                c = "#DCDCDC"; // 船员灵魂 => 亮灰色
                break;
            
            case RoleTypes.GuardianAngel:
                c = "#F08080"; // 天使 => 淡珊瑚
                break;
            
            case RoleTypes.ImpostorGhost:
                c = "#FFE4E1"; // 内鬼灵魂 => 薄雾玫瑰
                break;
            
        }

        return c;
    }
    public static Vector2 GetBlackRoomPS()
    {
        return GetPlayer.GetActiveMapId() switch
        {
            0 => new(-27f, 3.3f), // The Skeld
            1 => new(-11.4f, 8.2f), // MIRA HQ
            2 => new(42.6f, -19.9f), // Polus
            3 => new Vector2(27f, 3.3f), // dlekS ehT
            4 => new(-16.8f, -6.2f), // Airship
            5 => new(9.4f, 17.9f), // The Fungle
            _ => throw new System.NotImplementedException(),
        };
    }
    
    // Thanks TOHEN
    public static string GetHashedPuid(this ClientData player)
    {
        if (player == null) return "";
        string puid = player.ProductUserId;
        using SHA256 sha256 = SHA256.Create();
        
        // get sha-256 hash
        byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(puid));
        string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

        // pick front 5 and last 4
        return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
    }

    public static string GetPlatformText(this Platforms platform)
    {
        var color = platform switch
        {
            Platforms.StandaloneItch => "#FF4300",
            Platforms.StandaloneWin10 => "#FF7E32",
            Platforms.StandaloneEpicPC => "#FFD432",
            Platforms.StandaloneSteamPC => "#B8FF32",

            Platforms.Xbox => "#60FF32",
            Platforms.Switch => "#32FF69",
            Platforms.Playstation => "#32FFC6",

            Platforms.StandaloneMac => "#32E9FF",
            Platforms.IPhone => "#32AEFF",
            Platforms.Android => "#325AFF",

            Platforms.Unknown or
                _ => "#ffffff"
        };
        var platforms = platform switch
        {
            Platforms.StandaloneItch => "Itch",
            Platforms.StandaloneWin10 => "Windows",
            Platforms.StandaloneEpicPC => "Epic",
            Platforms.StandaloneSteamPC => "Steam",
            
            Platforms.Xbox => "Xbox",
            Platforms.Switch => "Switch",
            Platforms.Playstation => "PS",

            Platforms.StandaloneMac => "Mac",
            Platforms.IPhone => Translator.GetString("iPhone"),
            Platforms.Android => Translator.GetString("Android"),

            Platforms.Unknown or
                _ => Translator.GetString("Platforms.Unknown")
        };
        return $"<color={color}>{platforms}</color>";
    }

    public static string GetWinTeam(this GameOverReason gameOverReason)
    {
        switch (gameOverReason)
        {
            case GameOverReason.HumansByTask:
            case GameOverReason.HumansByVote:
            case GameOverReason.HideAndSeek_ByTimer:
                return "CrewmateWin";
            case GameOverReason.ImpostorByKill:
            case GameOverReason.ImpostorBySabotage:
            case GameOverReason.HideAndSeek_ByKills:
            case GameOverReason.ImpostorByVote:
                return "ImpostorsWin";
            case GameOverReason.HumansDisconnect:
            case GameOverReason.ImpostorDisconnect:
                return "NobodyWin";
        }

        return "ErrorWin";
    }
    public static Vector2 LocalPlayerLastTp;
    public static bool LocationLocked = false;
    public static void RpcTeleport(this PlayerControl player, Vector2 location)
    {
        Logger.Info($" {GetPlayer.GetNameRole(player)} => {location}", "RpcTeleport");
        Logger.Info($" Player Id: {player.PlayerId}", "RpcTeleport");
        if (player.inVent
            || player.MyPhysics.Animations.IsPlayingEnterVentAnimation())
        {
            Logger.Info($"Target: ({GetPlayer.GetNameRole(player)}) in vent", "RpcTeleport");
            player.MyPhysics.RpcBootFromVent(0);
        }
        if (player.onLadder
            || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            Logger.Warn($"Teleporting canceled - Target: ({GetPlayer.GetNameRole(player)}) is in on Ladder", "RpcTeleport");
            return;
        }
        var net = player.NetTransform;
        var numHost = (ushort)(net.lastSequenceId + 2);
        var numClient = (ushort)(net.lastSequenceId + 48);

        // Host side
        if (AmongUsClient.Instance.AmHost)
        {
            var playerlastSequenceId = (int)player.NetTransform.lastSequenceId;
            playerlastSequenceId += 10;
            player.NetTransform.SnapTo(location, (ushort)playerlastSequenceId);
            player.NetTransform.SnapTo(location, numHost);
        }
        else
        {
            // Local Teleport For Client
            MessageWriter localMessageWriter = AmongUsClient.Instance.StartRpcImmediately(net.NetId, (byte)RpcCalls.SnapTo, SendOption.None, player.GetClientId());
            NetHelpers.WriteVector2(location, localMessageWriter);
            localMessageWriter.Write(numClient);
            AmongUsClient.Instance.FinishRpcImmediately(localMessageWriter);
        }

        // For Client side
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(player.NetTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
        NetHelpers.WriteVector2(location, messageWriter);
        messageWriter.Write(player.NetTransform.lastSequenceId + 100U);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        // Global Teleport
        MessageWriter globalMessageWriter = AmongUsClient.Instance.StartRpcImmediately(net.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
        NetHelpers.WriteVector2(location, globalMessageWriter);
        globalMessageWriter.Write(numClient);
        AmongUsClient.Instance.FinishRpcImmediately(globalMessageWriter);

        if (PlayerControl.LocalPlayer == player)
            LocalPlayerLastTp = location;
    }
    
    public static void SendMessageAsPlayerImmediately(PlayerControl player, string text, bool hostCanSee = true, bool sendToModded = true)
    {
        Main.isChatCommand = true;
        if (hostCanSee) DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, text);
        if (!sendToModded) text += "\0";

        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
        writer.StartMessage(-1);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(text)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();
    }
    //public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", string.Empty);
    public static void KickPlayer(int playerId, bool ban, string reason)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKickReason, SendOption.Reliable, -1);
        writer.Write(GetString($"DCNotify.{reason}"));
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        _ = new LateTask(() =>
        {
            AmongUsClient.Instance.KickPlayer(playerId, ban);
        }, Math.Max(AmongUsClient.Instance.Ping / 500f, 1f), "Kick Player");
    }
    
    public static string getColoredPingText(int ping){

        if (ping <= 100){ // Green for ping < 100

            return $"<color=#00ff00ff>{ping}";//</color>";

        } else if (ping < 400){ // Yellow for 100 < ping < 400

            return $"<color=#ffff00ff>{ping}";//</color>";

        } else{ // Red for ping > 400

            return $"<color=#ff0000ff>{ping}";//</color>";
        }
    }
    public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    
    public static string getColoredFPSText(float fps)
    {
        string a = "";
        if (fps >= 100){ // Green for fps > 100

            return a + $"<color=#00ff00ff>{fps}";//</color>";

        } else if (fps < 100 & fps > 50){ // Yellow for 100 > fps > 50

            return a + $"<color=#ffff00ff>{fps}";//</color>";

        } else{ // Red for fps < 50

            return a + $"<color=#ff0000ff>{fps}";//</color>";
        }
    }
    public static KeyCode stringToKeycode(string keyCodeStr){

        if(!string.IsNullOrEmpty(keyCodeStr)){ // Empty strings are automatically invalid

            try{
                
                // Case-insensitive parse of UnityEngine.KeyCode to check if string is validssss
                KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), keyCodeStr, true);
                
                return keyCode;

            }catch{}
        
        }

        return KeyCode.Delete; // If string is invalid, return Delete as the default key
    }
    public static bool CheckBanList(string code, string puid = "")
    {
        bool OnlyCheckPuid = false;
        if (code == "" && puid != "") OnlyCheckPuid = true;
        else if (code == "") return false;

        string noDiscrim = "";
        if (code.Contains('#'))
        {
            int index = code.IndexOf('#');
            noDiscrim = code[..index];
        }

        try
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("YuEzTools.Resources.BlackList.txt");
            stream.Position = 0;
            using StreamReader sr = new(stream, Encoding.UTF8);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (!OnlyCheckPuid)
                {
                    if (line.IndexOf(code) >= 0) return true;
                    if (!string.IsNullOrEmpty(noDiscrim) && !line.Contains('#') && line.Contains(noDiscrim)) return true;
                }
                if (line.Contains(puid)) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }
    public static bool CheckFirstBanList(string code)
    {
        if (code == "") return false;

        string noDiscrim = "";
        if (code.Contains('#'))
        {
            int index = code.IndexOf('#');
            noDiscrim = code[..index];
        }

        try
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("YuEzTools.Resources.BlackFirstList.txt");
            stream.Position = 0;
            using StreamReader sr = new(stream, Encoding.UTF8);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (line.IndexOf(noDiscrim) >= 0) return true;
                if (noDiscrim.IndexOf(line) >= 0) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }
    private static readonly string BAN_LIST_PATH = @"./YuET_Data/BanList.txt";
    public static bool CheckBanner(string code, string puid = "")
    {
        bool OnlyCheckPuid = false;
        if (code == "" && puid != "") OnlyCheckPuid = true;
        else if (code == "") return false;

        string noDiscrim = "";
        if (code.Contains('#'))
        {
            int index = code.IndexOf('#');
            noDiscrim = code[..index];
        }

        try
        {
            Directory.CreateDirectory("YuET_Data");
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            using StreamReader sr = new(BAN_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (!OnlyCheckPuid)
                {
                    if (line.IndexOf(code) >= 0) return true;
                    if (!string.IsNullOrEmpty(noDiscrim) && !line.Contains('#') && line.Contains(noDiscrim)) return true;
                }
                if (line.Contains(puid)) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }
}