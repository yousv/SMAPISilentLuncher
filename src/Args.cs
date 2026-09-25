using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SmapiLauncher;

static partial class Launcher
{
    const string ManifestFile = "manifest.json";
    const string DefaultModsDir = "Mods";
    const int MaxLaunchArgs = 32767;
    const int MaxScanDirs = 256;
    const int ManifestBufSize = 4096;

    static string ForwardedArgs()
    {
        var tokens = Tokenize(Environment.CommandLine);
        var kept = new List<(int Start, int Length)>();
        for (int i = 1; i < tokens.Count; i++)
            kept.Add((tokens[i].Start, tokens[i].Length));
        return Rebuild(Environment.CommandLine, kept);
    }

    static void ParseModsPath(string args)
    {
        var tokens = Tokenize(args);
        for (int i = 0; i < tokens.Count; i++)
        {
            string text = tokens[i].Text;
            if (text.Length < 11 || !text.StartsWith("--mods-path", StringComparison.OrdinalIgnoreCase))
                continue;
            string rest = text.Substring(11);
            string val;
            if (rest.Length == 0)
            {
                if (i + 1 >= tokens.Count) return;
                val = tokens[i + 1].Text;
            }
            else if (rest[0] == '=' || rest[0] == ':')
            {
                val = rest.Substring(1);
                if (val.Length == 0)
                {
                    if (i + 1 >= tokens.Count) return;
                    val = tokens[i + 1].Text;
                }
            }
            else continue;
            if (val.Length > 0) modsPath = val;
            return;
        }
    }

    readonly struct ArgToken
    {
        internal readonly string Text;
        internal readonly int Start;
        internal readonly int Length;
        internal ArgToken(string text, int start, int length) { Text = text; Start = start; Length = length; }
    }

    static List<ArgToken> Tokenize(string s)
    {
        var tokens = new List<ArgToken>();
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
            if (i >= s.Length) break;
            int start = i;
            var text = new StringBuilder();
            while (i < s.Length && s[i] != ' ' && s[i] != '\t')
            {
                if (s[i] == '"')
                {
                    i++;
                    while (i < s.Length && s[i] != '"')
                        text.Append(s[i++]);
                    if (i < s.Length) i++;
                }
                else text.Append(s[i++]);
            }
            tokens.Add(new ArgToken(text.ToString(), start, i - start));
        }
        return tokens;
    }

    static string Rebuild(string s, List<(int Start, int Length)> kept)
    {
        var out_ = new StringBuilder();
        for (int i = 0; i < kept.Count; i++)
        {
            if (i > 0) out_.Append(' ');
            out_.Append(s.Substring(kept[i].Start, kept[i].Length));
        }
        return out_.ToString();
    }

    static bool IsProcessRunning(string name)
    {
        return Native.HasProcess(name);
    }

    static void CountMods()
    {
        string resolved = Path.IsPathRooted(modsPath) ? modsPath : Path.Combine(launcherDir, modsPath);
        if (!Directory.Exists(resolved)) { overlayMessage = "Launching SMAPI"; return; }

        int mods = 0, packs = 0, scanned = 0;
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(resolved))
            {
                if (scanned++ >= MaxScanDirs) break;
                CountInFolder(dir, ref mods, ref packs);
                foreach (string sub in Directory.EnumerateDirectories(dir))
                {
                    if (scanned++ >= MaxScanDirs) break;
                    CountInFolder(sub, ref mods, ref packs);
                }
            }
        }
        catch { }

        int total = mods + packs;
        if (mods > 0 && packs > 0)
            overlayMessage = $"Launching SMAPI with {mods} mods, {packs} content packs ({total})";
        else if (mods > 0)
            overlayMessage = $"Launching SMAPI with {mods} mods ({total})";
        else if (packs > 0)
            overlayMessage = $"Launching SMAPI with {packs} content packs ({total})";
        else
            overlayMessage = "Launching SMAPI";
    }

    static void CountInFolder(string folder, ref int mods, ref int packs)
    {
        string manifest = Path.Combine(folder, ManifestFile);
        if (!File.Exists(manifest)) return;
        string content;
        try
        {
            using FileStream stream = File.OpenRead(manifest);
            byte[] buffer = new byte[ManifestBufSize];
            int length = 0;
            while (length < buffer.Length)
            {
                int read = stream.Read(buffer, length, buffer.Length - length);
                if (read <= 0) break;
                length += read;
            }
            content = Encoding.UTF8.GetString(buffer, 0, length);
        }
        catch { return; }
        if (content.Contains("\"EntryDll\"")) mods++;
        else if (content.Contains("\"ContentPackFor\"")) packs++;
    }
}
