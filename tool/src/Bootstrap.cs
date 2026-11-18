// Bootstrap.cs —— 插件入口 / 日志 / 延迟打补丁
// 由 Patcher 插入到游戏入口调用 Init()。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace ProjectionMod
{
    public static class Bootstrap
    {
        public static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectionMod.log");

        /// <summary>
        /// 游戏入口最前面执行，此时游戏尚未注册内嵌程序集(ReLogic/Steamworks)的
        /// AssemblyResolve 处理器。
        /// 【严禁】在这里直接触碰 Terraria.* 类型或打 Harmony 补丁——
        /// 否则类型初始化会在依赖可解析前失败并被永久缓存，导致启动崩溃。
        /// 正确做法：起后台线程，等游戏就绪后重试打补丁。
        /// </summary>
        public static void Init()
        {
            try
            {
                Log("=== ProjectionMod init ===");
                Log("runtime: " + Environment.Version);
                Thread t = new Thread(PatchWhenReady);
                t.IsBackground = true;
                t.Start();

                Thread w = new Thread(Watchdog);
                w.IsBackground = true;
                w.Start();
            }
            catch (Exception e)
            {
                TryLog("Init FAILED: " + e);
            }
        }

        /// <summary>
        /// 看门狗：主线程超过 5 秒没 Tick 就在日志里报 STALL。
        /// 不能抓托管栈，但能留下“卡死发生 + 时间点”的证据，便于下次定位。
        /// </summary>
        private static void Watchdog()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(2000);
                    DateTime last = Core.LastTickUtc;
                    if (last == DateTime.MinValue) continue;
                    double lag = (DateTime.UtcNow - last).TotalSeconds;
                    if (lag > 5)
                    {
                        TryLog("STALL 主线程疑似卡死 " + lag.ToString("F1") + " 秒（看门狗）");
                        Thread.Sleep(8000);
                    }
                }
            }
            catch { /* 看门狗自身异常忽略 */ }
        }

        /// <summary>每秒重试 PatchAll，直到游戏内嵌程序集可解析。</summary>
        private static void PatchWhenReady()
        {
            for (int attempt = 1; attempt <= 60; attempt++)
            {
                try
                {
                    Harmony harmony = new Harmony("com.projectionmod.terraria");
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                    GameHooks.SetHarmony(harmony);
                    Log("Harmony patches applied OK (attempt " + attempt + ")");
                    return;
                }
                catch (Exception e)
                {
                    if (attempt == 1 || attempt % 10 == 0)
                        TryLog("patch attempt " + attempt + " not ready: " + e.Message);
                }
                Thread.Sleep(1000);
            }
            TryLog("patch failed after 60 attempts");
        }

        public static void Log(string msg) { TryLog(msg); }

        private static void TryLog(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + "\r\n");
            }
            catch { /* 日志失败不影响游戏 */ }
        }
    }
}
