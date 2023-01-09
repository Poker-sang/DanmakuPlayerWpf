using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DanmakuPlayer.Enums;
using DanmakuPlayer.Models;
using Microsoft.International.Converters.PinYinConverter;

namespace DanmakuPlayer.Services;

public static class DanmakuCombiner
{
    public const int MaxCosine = 60;
    public const int MinDanmakuSize = 10;
    public const int MaxDist = 5;
    public const int Threshold = 20;
    public const bool CrossMode = false;
    public const int RepresentativePercent = 50;

    /// <summary>
    /// 全角字符和部分英文标点
    /// </summary>
    private const string FullAngleChars = "　１２３４５６７８９０!＠＃＄％＾＆＊（）－＝＿＋［］｛｝;＇:＂,．／＜＞?＼｜｀～ｑｗｅｒｔｙｕｉｏｐａｓｄｆｇｈｊｋｌｚｘｃｖｂｎｍＱＷＥＲＴＹＵＩＯＰＡＳＤＦＧＨＪＫＬＺＸＣＶＢＮＭ";

    /// <summary>
    /// 半角字符和部分中文标点
    /// </summary>
    private const string HalfAngleChars = @" 1234567890！@#$%^&*()-=_+[]{}；\'：""，./<>？\\|`~qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBN";

    private static int Hash(char a, char b) => ((a << 10) ^ b) & 0xFFFFF;

    private static List<int> Gen2GramArray(string p)
    {
        p += p[0];
        var res = new List<int>();
        for (var i = 0; i < p.Length - 1; ++i)
            res.Add(Hash(p[i], p[i + 1]));
        return res;
    }

    private static string ToSubscript(uint x) => x is 0 ? "" : ToSubscript(x / 10) + (char)(0x2080 + x % 10);

    /// <remarks>abnormal edit distance</remarks>>
    private static int EditDistance(string p, string q)
    {
        var edCounts = new Dictionary<char, int>();
        foreach (var t in p)
            edCounts[t] = edCounts.GetValueOrDefault(t, 0) + 1;

        foreach (var t in q)
            edCounts[t] = edCounts.GetValueOrDefault(t, 0) - 1;


        return edCounts.Values.Sum(Math.Abs);
    }

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

    private static bool Similarity(DanmakuString p, DanmakuString q)
    {
        if (p.Original == q.Original)
            return true;

        var dis = EditDistance(p.Original, q.Original);
        if ((p.Length + q.Length < MinDanmakuSize) ? dis < (p.Length + q.Length) / MinDanmakuSize * MaxDist - 1 : dis <= MaxDist)
            return true;

        var pyDis = EditDistance(p.Pinyin, q.Pinyin);
        if ((p.Length + q.Length < MinDanmakuSize) ? pyDis < (p.Length + q.Length) / MinDanmakuSize * MaxDist - 1 : pyDis <= MaxDist)
            return true;

        // they have nothing similar. CosineDistance test can be bypassed
        if (dis >= p.Length + q.Length)
            return false;

        var cos = CosineDistance(p.Gram, q.Gram) * 100;
        if (cos >= MaxCosine)
            return true;

        return false;
    }

    private record DanmakuString(string Original, int Length, string Pinyin, List<int> Gram);

    public static List<Danmaku> Combine(IEnumerable<Danmaku> pool)
    {
        var danmakuChunk = new List<(DanmakuString Str, List<Danmaku> Peers)>();
        var outDanmaku = new List<List<Danmaku>>();

        // var i = 0;
        foreach (var danmaku in pool.Where(danmaku => danmaku.Mode is DanmakuMode.Roll or DanmakuMode.Top or DanmakuMode.Bottom))
        {
            // ++i;
            // Debug.WriteLine(i);
            var pinyin = "";
            foreach (var c in danmaku.Text)
                if (ChineseChar.IsValidChar(c))
                    pinyin += new ChineseChar(c).Pinyins[0];
                else
                    pinyin += c;

            var str = new DanmakuString(danmaku.Text, danmaku.Text.Length, pinyin, Gen2GramArray(danmaku.Text));
            while (danmakuChunk.Count > 0 && danmaku.Time - danmakuChunk[0].Peers[0].Time > Threshold)
            {
                outDanmaku.Add(danmakuChunk[0].Peers);
                danmakuChunk.RemoveAt(0);
            }

            var addNew = true;
            foreach (var (_, peers) in danmakuChunk
                         .Where(chunk => CrossMode || danmaku.Mode == chunk.Peers[0].Mode)
                         .Where(chunk => Similarity(str, chunk.Str)))
            {
                peers.Add(danmaku);
                addNew = false;
                break;
            }
            if (addNew)
                danmakuChunk.Add((str, new List<Danmaku> { danmaku }));
        }
        while (danmakuChunk.Count > 0)
        {
            outDanmaku.Add(danmakuChunk[0].Peers);
            danmakuChunk.RemoveAt(0);
        }

        var ret = new List<Danmaku>();
        foreach (var peers in outDanmaku)
        {
            if (peers.Count is 1)
            {
                ret.Add(peers[0]);
                continue;
            }
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

            var represent = new Danmaku(
                (peers.Count > 5 ? $"₍{ToSubscript((uint)peers.Count)}₎" : "") + peers[0].Text,
                peers[Math.Min(peers.Count * RepresentativePercent / 100, peers.Count - 1)].Time,
                mode,
                25 * (peers.Count <= 5 ? 1 : (int)Math.Log(peers.Count, 5)),
                (uint)peers.Average(t => t.Color),
                0,
                DanmakuPool.Normal,
                ""
            );
            ret.Add(represent);
        }

        return ret;
    }
}
