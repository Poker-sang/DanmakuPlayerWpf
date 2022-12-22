using DanmakuPlayer.Services;
using System.Text.Json;
using System.Threading.Tasks;

namespace DanmakuPlayer.Resources;

public static class BiliApis
{
    public const string VideoPageList = "https://api.bilibili.com/x/player/pagelist?";

    public static Task<JsonDocument> GetVideoPageList(string bv) => $"{VideoPageList}bvid={bv}".DownloadJsonAsync();
    public static Task<JsonDocument> GetVideoPageList(int av) => $"{VideoPageList}aid={av}".DownloadJsonAsync();

    /// <remarks>
    /// https://api.bilibili.com/x/v1/dm/list.so?oid=
    /// </remarks>
    public const string DanmakuFromCid = "http://comment.bilibili.com/";

    public static Task<string> GetDanmaku(int cid) => $"{DanmakuFromCid}{cid}.xml".DownloadStringAsync();

    public const string BangumiInfo = "https://api.bilibili.com/pgc/review/user?";

    public static Task<JsonDocument> GetBangumiInfo(int mediaId) => $"{BangumiInfo}media_id={mediaId}".DownloadJsonAsync();

    public const string BangumiEpisode = "https://api.bilibili.com/pgc/web/season/section?";

    public static Task<JsonDocument> GetBangumiEpisode(int seasonId) => $"{BangumiEpisode}season_id={seasonId}".DownloadJsonAsync();

    public const string BangumiEpisodeInfo = "https://api.bilibili.com/pgc/season/episode/web/info?";

    public static Task<JsonDocument> GetBangumiEpisodeInfo(int episodeId) => $"{BangumiEpisodeInfo}ep_id={episodeId}".DownloadJsonAsync();
}
