namespace Maigret.Net.Core.Constants;

/// <summary>
/// Identifier types Maigret can search by. Mirrors <c>SUPPORTED_IDS</c> in
/// <c>checking.py</c>; sites declare which one they accept via <c>type</c>.
/// </summary>
public static class IdTypes
{
    public const string Username = "username";
    public const string YandexPublicId = "yandex_public_id";
    public const string GaiaId = "gaia_id";
    public const string VkId = "vk_id";
    public const string OkId = "ok_id";
    public const string WikimapiaUid = "wikimapia_uid";
    public const string SteamId = "steam_id";
    public const string UidmeUguid = "uidme_uguid";
    public const string YelpUserId = "yelp_userid";
}
