// Compat.cs —— 兼容层：反射成员集中解析 + 启动自检
// 目的：游戏小版本更新时（字段/方法改名、移动）能第一时间从日志看出“哪一项没找到”，
// 而不是整块功能静默失效。所有反射解析都应走这里。
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProjectionMod
{
    public static class Compat
    {
        private static readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private static readonly List<string> _missing = new List<string>();
        private static bool _checked;

        public static void Missing(string what)
        {
            string key = "miss:" + what;
            if (_cache.ContainsKey(key)) return;
            _cache[key] = null;
            _missing.Add(what);
            Bootstrap.Log("兼容自检: 未找到 " + what + "（游戏版本可能变化，该项功能会降级）");
        }

        public static List<string> MissingList { get { return _missing; } }

        // ===== 字段 =====
        public static FieldInfo Field(Type t, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Static)
        {
            string key = "F:" + t.FullName + ":" + name;
            object hit;
            if (_cache.TryGetValue(key, out hit)) return hit as FieldInfo;
            FieldInfo f = null;
            try { f = t.GetField(name, flags); }
            catch { }
            _cache[key] = f;
            if (f == null) Missing(t.Name + "::" + name);
            return f;
        }

        // ===== 属性 =====
        public static PropertyInfo Property(Type t, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            string key = "P:" + t.FullName + ":" + name;
            object hit;
            if (_cache.TryGetValue(key, out hit)) return hit as PropertyInfo;
            PropertyInfo p = null;
            try { p = t.GetProperty(name, flags); }
            catch { }
            _cache[key] = p;
            if (p == null) Missing(t.Name + "::" + name + " (属性)");
            return p;
        }

        // ===== 方法（按签名）=====
        public static MethodInfo Method(Type t, string name, params Type[] args)
        {
            string key = "M:" + t.FullName + ":" + name + ":" + args.Length;
            object hit;
            if (_cache.TryGetValue(key, out hit)) return hit as MethodInfo;
            MethodInfo m = null;
            try { m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, args, null); }
            catch { }
            _cache[key] = m;
            if (m == null) Missing(t.Name + "::" + name + "/" + args.Length);
            return m;
        }

        /// <summary>取任意同名方法（参数不确定时用）。</summary>
        public static MethodInfo AnyMethod(Type t, string name)
        {
            string key = "MA:" + t.FullName + ":" + name;
            object hit;
            if (_cache.TryGetValue(key, out hit)) return hit as MethodInfo;
            MethodInfo m = null;
            try
            {
                MethodInfo[] all = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].Name == name) { m = all[i]; break; }
                }
            }
            catch { }
            _cache[key] = m;
            if (m == null) Missing(t.Name + "::" + name + "()");
            return m;
        }

        /// <summary>启动自检：检查本插件依赖的关键成员，输出一条汇总日志。</summary>
        public static void SelfCheck()
        {
            if (_checked) return;
            _checked = true;
            try
            {
                // 关键反射点：材质资源表、资源共享属性、内容样本表、绘制入口
                Field(typeof(Terraria.GameContent.TextureAssets), "Tile");
                Field(typeof(Terraria.GameContent.TextureAssets), "Wall");
                Field(typeof(Terraria.GameContent.TextureAssets), "Item");
                Field(typeof(Terraria.ID.ContentSamples), "ItemsByType");
                AnyMethod(typeof(Terraria.Main), "DrawInterface");
                AnyMethod(typeof(Terraria.Player), "PlaceThing");
                AnyMethod(typeof(Terraria.WorldGen), "PlaceTile");
                Property(typeof(Terraria.Player), "ItemTimeIsZero");
                AnyMethod(typeof(Terraria.Main), "LoadTiles");
                AnyMethod(typeof(Terraria.Main), "LoadWall");
                AnyMethod(typeof(Terraria.Main), "LoadItem");

                int total = 11;
                int bad = _missing.Count;
                Bootstrap.Log("兼容自检: " + (total - bad) + "/" + total + " 项通过" +
                    (bad > 0 ? ("，缺失 " + bad + " 项（见上方日志）") : "，全部正常"));
            }
            catch (Exception e)
            {
                Bootstrap.Log("兼容自检异常: " + e.Message);
            }
        }
    }
}
