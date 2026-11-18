// Patcher.cs —— 泰拉瑞亚 1.4.5.8 投影 Mod 打补丁工具
// 用 Mono.Cecil 在游戏入口 (Terraria.WindowsLaunch.Main) 开头插入 ProjectionMod.Bootstrap.Init()。
// 用法: Patcher <status|backup|patch|restore|test> [游戏目录] [输出目录]
//   status   : 查看状态（是否已打补丁 / 是否有备份）
//   backup   : 备份 Terraria.exe -> Terraria_backup_projection.exe
//   patch    : 自动备份 + 打补丁 + 原子替换（给了输出目录则只把成品写到那里，不碰游戏目录）
//   restore  : 用备份还原
//   test     : 打补丁写入副本 (Terraria.exe.testpatch.exe)，不碰原文件
// 兼容 .NET Framework 4.0（C# 5 语法）。

using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ProjectionPatcher
{
    public static class Patcher
    {
        public static int Main(string[] args)
        {
            string gameDir = @"d:\steam\steamapps\common\Terraria";
            string cmd = "status";
            string outDir = null;
            if (args.Length >= 1) cmd = args[0].ToLowerInvariant();
            if (args.Length >= 2) gameDir = args[1];
            if (args.Length >= 3) outDir = args[2];

            string exe = Path.Combine(gameDir, "Terraria.exe");
            string backup = Path.Combine(gameDir, "Terraria_backup_projection.exe");

            try
            {
                switch (cmd)
                {
                    case "status":  return Status(exe, backup);
                    case "backup":  return Backup(exe, backup);
                    case "patch":   return Patch(exe, backup, outDir);
                    case "restore": return Restore(exe, backup);
                    case "test":    return TestPatch(exe, outDir);
                    default:
                        Console.WriteLine("用法: Patcher [status|backup|patch|restore|test] [游戏目录] [输出目录]");
                        return 2;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("出错: " + e.ToString());
                return 1;
            }
        }

        private static bool IsPatched(string exe)
        {
            AssemblyDefinition ad = AssemblyDefinition.ReadAssembly(exe);
            try
            {
                foreach (AssemblyNameReference r in ad.MainModule.AssemblyReferences)
                    if (r.Name == "ProjectionMod") return true;
                return false;
            }
            finally { ad.Dispose(); }
        }

        private static int Status(string exe, string backup)
        {
            Console.WriteLine("Terraria.exe : " + (File.Exists(exe) ? "存在" : "不存在"));
            Console.WriteLine("已打补丁     : " + (File.Exists(exe) && IsPatched(exe)));
            Console.WriteLine("投影备份     : " + (File.Exists(backup) ? "存在" : "不存在"));
            return 0;
        }

        private static int Backup(string exe, string backup)
        {
            if (File.Exists(backup)) { Console.WriteLine("备份已存在，跳过"); return 0; }
            File.Copy(exe, backup);
            Console.WriteLine("已备份 -> " + Path.GetFileName(backup));
            return 0;
        }

        private static MethodDefinition FindInit(string modDll)
        {
            AssemblyDefinition modAd = AssemblyDefinition.ReadAssembly(modDll);
            try
            {
                foreach (TypeDefinition t in modAd.MainModule.Types)
                {
                    if (t.FullName != "ProjectionMod.Bootstrap") continue;
                    foreach (MethodDefinition m in t.Methods)
                        if (m.Name == "Init" && m.Parameters.Count == 0) return m;
                }
                return null;
            }
            finally { modAd.Dispose(); }
        }

        private static bool InjectInit(AssemblyDefinition ad, string modDll, out string err)
        {
            err = null;
            MethodDefinition ep = ad.EntryPoint;
            if (ep == null || !ep.HasBody) { err = "找不到入口点"; return false; }
            MethodDefinition init = FindInit(modDll);
            if (init == null) { err = "找不到 ProjectionMod.Bootstrap.Init"; return false; }
            MethodReference initRef = ad.MainModule.ImportReference(init);
            ILProcessor il = ep.Body.GetILProcessor();
            Instruction first = ep.Body.Instructions[0];
            il.InsertBefore(first, il.Create(OpCodes.Call, initRef));
            return true;
        }

        private static int Patch(string exe, string backup, string outDir)
        {
            if (!File.Exists(exe)) { Console.WriteLine("找不到 Terraria.exe"); return 1; }
            if (IsPatched(exe)) { Console.WriteLine("已经打过补丁"); return 0; }

            string modDll = Path.Combine(Path.GetDirectoryName(typeof(Patcher).Assembly.Location), "ProjectionMod.dll");
            if (!File.Exists(modDll)) { Console.WriteLine("缺少 " + modDll); return 1; }

            string output = outDir != null
                ? Path.Combine(outDir, "Terraria.exe")
                : exe;

            string tmp = output + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp);

            AssemblyDefinition ad = AssemblyDefinition.ReadAssembly(exe);
            try
            {
                string err;
                if (!InjectInit(ad, modDll, out err)) { Console.WriteLine(err); return 1; }
                ad.Write(tmp);
            }
            finally { ad.Dispose(); }

            if (outDir != null)
            {
                Console.WriteLine("补丁成品已写到: " + tmp);
                Console.WriteLine("（由部署脚本负责备份/替换，此处不碰游戏目录）");
                return 0;
            }

            Backup(exe, backup);
            try { File.Replace(tmp, exe, null); }
            catch { File.Copy(tmp, exe, true); File.Delete(tmp); }

            Console.WriteLine("打补丁完成，入口: " + Path.GetFileName(exe));
            return 0;
        }

        private static int Restore(string exe, string backup)
        {
            if (!File.Exists(backup)) { Console.WriteLine("没有备份，无法还原"); return 1; }
            File.Copy(backup, exe, true);
            Console.WriteLine("已还原原版 Terraria.exe");
            return 0;
        }

        private static int TestPatch(string exe, string outDir)
        {
            string dir = outDir ?? Path.GetDirectoryName(exe);
            string tmp = Path.Combine(dir, "Terraria.exe.testpatch.exe");
            string modDll = Path.Combine(Path.GetDirectoryName(typeof(Patcher).Assembly.Location), "ProjectionMod.dll");
            if (!File.Exists(modDll)) { Console.WriteLine("缺少 " + modDll); return 1; }
            if (File.Exists(tmp)) File.Delete(tmp);

            AssemblyDefinition ad = AssemblyDefinition.ReadAssembly(exe);
            try
            {
                string err;
                if (!InjectInit(ad, modDll, out err)) { Console.WriteLine(err); return 1; }
                ad.Write(tmp);
            }
            finally { ad.Dispose(); }

            Console.WriteLine("测试副本: " + tmp);
            AssemblyDefinition chk = AssemblyDefinition.ReadAssembly(tmp);
            try
            {
                Console.WriteLine("入口点  : " + chk.EntryPoint.FullName);
                Console.WriteLine("前 4 条指令:");
                int i = 0;
                foreach (Instruction ins in chk.EntryPoint.Body.Instructions)
                {
                    Console.WriteLine("  " + ins);
                    if (++i >= 4) break;
                }
                bool has = false;
                foreach (AssemblyNameReference r in chk.MainModule.AssemblyReferences)
                    if (r.Name == "ProjectionMod") has = true;
                Console.WriteLine("AssemblyRef 含 ProjectionMod: " + has);
            }
            finally { chk.Dispose(); }
            return 0;
        }
    }
}
