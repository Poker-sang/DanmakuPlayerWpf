using System;
using System.Collections.Generic;
using System.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using Microsoft.International.Converters.PinYinConverter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DanmakuPlayer.Services;

public static class DanmakuCombiner
{
    private static int EditDistance(string p, string q)
    {
        var edCounts = new Dictionary<char, int>();
        foreach (var t in p)
            edCounts[t] = edCounts.GetValueOrDefault(t, 0) + 1;

        foreach (var t in q)
            edCounts[t] = edCounts.GetValueOrDefault(t, 0) - 1;


        return edCounts.Values.Sum(Math.Abs);
    }

    private static List<int> Gen2GramArray(string p)
    {
        p += p[0];
        var res = new List<int>();
        for (var i = 0; i < p.Length - 1; ++i)
            res.Add(Hash(p[i], p[i + 1]));
        return res;
    }

    private static int Hash(char a, char b) => ((a << 10) ^ b) & 0xFFFFF;

    public const int MaxCosine = 60;
    public const int MinDanmakuSize = 10;
    public const int MaxDist = 5;

    private static double CosineDistance(List<int> p, List<int> q)
    {
        var edA = new Dictionary<int, int>();
        var edB = new Dictionary<int, int>();
        foreach (var t in p)
            edA[t] = edA.GetValueOrDefault(t, 0) + 1;

        foreach (var t in q)
            edB[t] = edB.GetValueOrDefault(t, 0) + 1;

        var x = 0;
        var y = 0;
        var z = 0;

        foreach (var h1 in p.Where(h1 => edA[h1] is 0))
        {
            y += edA[h1] * edA[h1];
            if (edB[h1] is 0)
            {
                x += edA[h1] * edB[h1];
                z += edB[h1] * edB[h1];
                edB[h1] = 0;
            }
            edA[h1] = 0;
        }

        foreach (var h1 in q.Where(h1 => edB[h1] is 0))
        {
            z += edB[h1] * edB[h1];
            edB[h1] = 0;
        }

        return (double)x * x / (y * z);
    }
    private static string? Similarity(string p, string q, Status s)
    {
        if (p == q)
        {
            ++s.Identical;
            return "==";
        }

        var pGram = Gen2GramArray(p);
        var qGram = Gen2GramArray(q);
        var pPinYin = "";
        var qPinYin = "";
        foreach (var c in p)
        {
            if (ChineseChar.IsValidChar(c))
                pPinYin += new ChineseChar(c).Pinyins[0];
            else
                pPinYin += c;
        }
        foreach (var c in q)
        {
            if (ChineseChar.IsValidChar(c))
                qPinYin += new ChineseChar(c).Pinyins[0];
            else
                qPinYin += c;
        }

        var dis = EditDistance(p, q);
        if ((p.Length + q.Length < MinDanmakuSize) ? dis < (p.Length + q.Length) / MinDanmakuSize * MaxDist - 1 : dis <= MaxDist)
        {
            s.EditDistance++;
            return "<=" + dis;
        }

        var pyDis = EditDistance(pPinYin, qPinYin);
        if ((p.Length + q.Length < MinDanmakuSize) ? pyDis < (p.Length + q.Length) / MinDanmakuSize * MaxDist - 1 : pyDis <= MaxDist)
        {
            s.PinyinDistance++;
            return "P<=" + pyDis;
        }

        if (dis >= p.Length + q.Length) // they have nothing similar. cosine_distance test can be bypassed
            return null;
        var cos = CosineDistance(pGram, qGram) * 100;
        if (cos >= MaxCosine)
        {
            s.CosineDistance++;
            return cos + "%";
        }
        return null;
    }

    public const int Threshold = 20;
    public const bool CrossMode = false;
    public const int RepresentativePercent = 50;

    public static void Combine(List<Danmaku> pool)
    {
        var s = new Status();
        var danmakuChunk = new List<List<Danmaku>>();
        var outDanmaku = new List<List<Danmaku>>();
        var ret = new List<(Danmaku Represent, List<Danmaku> Peers)>();

        foreach (var danmaku in pool.Where(danmaku => danmaku.Mode is DanmakuMode.Roll or DanmakuMode.Top or DanmakuMode.Bottom))
        {
            while (danmakuChunk.Count > 0 && danmaku.Time - danmakuChunk[0][0].Time > Threshold)
            {
                outDanmaku.Add(danmakuChunk[0]);
                danmakuChunk.RemoveAt(0);
            }

            foreach (var another in danmakuChunk)
            {
                var first = another[0];
                if (CrossMode && danmaku.Mode != first.Mode)
                    continue;
                var sim = Similarity(danmaku.Text, first.Text, s);
                if (sim is not null)
                    another.Add(danmaku);
            }
            danmakuChunk.Add(new List<Danmaku> { danmaku });
        }
        while (danmakuChunk.Count > 0)
        {
            outDanmaku.Add(danmakuChunk[0]);
            danmakuChunk.RemoveAt(0);
        }

        // apply representative if is not the first one
        foreach (var peers in outDanmaku)
        {
            var mode = DanmakuMode.Bottom;
            foreach (var danmaku in peers)
            {
                switch (danmaku.Mode)
                {
                    case DanmakuMode.Roll:
                    case DanmakuMode.Top when mode is DanmakuMode.Bottom:
                        mode = danmaku.Mode;
                        break;
                    default: break;
                }
            }
            var represent = peers[0] with
            {
                Mode = mode,
                Time = peers[Math.Min((int)Math.Floor(peers.Count * RepresentativePercent / 100f), peers.Count - 1)].Time
            };
        }
    }
}

public class Status
{
    public int Identical { get; set; }// combined
    public int EditDistance { get; set; }
    public int PinyinDistance { get; set; }
    public int CosineDistance { get; set; }
    public int Blacklist { get; set; } // deleted
    public int CountHide { get; set; }
    public int Whitelist { get; set; }// ignored
    public int BatchIgnore { get; set; }
    public int Script { get; set; }
    public int Enlarge { get; set; }// modified
    public int Shrink { get; set; }
    public int Scroll { get; set; }
    public int Taolu { get; set; }// other
    public int Total { get; set; }
    public int Onscreen { get; set; }
    public int Maxcombo { get; set; }
    public int Maxdispval { get; set; }
    public int ParseTimeMs { get; set; }

}
