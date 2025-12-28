using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable AsyncVoidMethod

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace IHateCheaters.Models
{
    public abstract class CheaterHandler : MonoBehaviour
    {
        private static Dictionary<string, ModData>? ModList = new();
        private const string ModsEndpoint = "https://api.aeris.now/mods";

        private static readonly Dictionary<string, string[]> CosmeticList = new()
        {
            { "LBAAD.", ["Admin Badge", "4A1B82", "FF69B4"] },
            { "LBAAK.", ["Dev Stick", "FF0000", "6E2323"] },
            { "LMAPY.", ["Forest Guide", "206DB0", "284ABD"] },
            { "LBAGS.", ["Illustrator", "E100FF", "BF7F0F"] },
            { "LBADE.", ["Finger Painter", "FF0000", "00FF00", "0000FF"] },
            { "LBANI.", ["Another Axiom Creator", "4A1B82", "1C8A53"] }
        };

        private static bool modsLoaded;

        private async void Start()
        {
            await LoadMods();
        }

        private static async Task LoadMods()
        {
            if (modsLoaded) return;

            using var req = UnityWebRequest.Get(ModsEndpoint);
            req.downloadHandler = new DownloadHandlerBuffer();
            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) return;

            var json = req.downloadHandler.text;
            ModList = JsonConvert.DeserializeObject<Dictionary<string, ModData>>(json);

            modsLoaded = true;
        }

        private static string ApplyGradient(string text, params string[] hexColors)
        {
            if (string.IsNullOrEmpty(text) || hexColors.Length == 0) return text;

            var result = "";
            var len = text.Length;

            for (var i = 0; i < len; i++)
            {
                var t = (float)i / (len - 1);
                Color color = Color.white;

                if (hexColors.Length == 2)
                {
                    ColorUtility.TryParseHtmlString("#" + hexColors[0], out var a);
                    ColorUtility.TryParseHtmlString("#" + hexColors[1], out var b);
                    color = Color.Lerp(a, b, t);
                }
                else if (hexColors.Length == 3)
                {
                    ColorUtility.TryParseHtmlString("#" + hexColors[0], out var a);
                    ColorUtility.TryParseHtmlString("#" + hexColors[1], out var b);
                    ColorUtility.TryParseHtmlString("#" + hexColors[2], out var c);
                    color = t < 0.5f ? Color.Lerp(a, b, t * 2f) : Color.Lerp(b, c, (t - 0.5f) * 2f);
                }

                result += $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text[i]}</color>";
            }

            return result;
        }

        public static async Task<string?> IsCheating(NetPlayer? player)
        {
            var totalFps = 0;
            var checkFrames = 30;

            try
            {
                var rig = GetVRRigFromPlayer(player);
                if (!rig) return null;

                var playerRef = player?.GetPlayerRef();
                if (ModList == null) return null;

                var detectedMods = ModList
                    .Where(m => playerRef != null && playerRef.CustomProperties.ContainsKey(m.Key))
                    .Select(m => $"<color=#{m.Value.Color}>{m.Value.Name}</color>")
                    .ToList();

                var allowedCosmetics = rig.concatStringOfCosmeticsAllowed;
                var ownedCosmetics = CosmeticList
                    .Where(c => allowedCosmetics.Contains(c.Key))
                    .Select(c => ApplyGradient(c.Value[0], c.Value.Skip(1).ToArray()))
                    .ToList();

                var cosmeticSet = rig.cosmeticSet;
                bool hasCosmetX = cosmeticSet.items.Any(c => !c.isNullItem && !rig.concatStringOfCosmeticsAllowed.Contains(c.itemName));
                if (hasCosmetX && !rig.inTryOnRoom)
                    detectedMods.Add("<color=#d91111>CosmetX</color>");
                
                var fpsField = Traverse.Create(rig).Field("fps");
                for (var i = 0; i < checkFrames; i++)
                {
                    if (fpsField.GetValue() is int fps)
                        totalFps += fps;

                    await Task.Delay(100);
                }

                var avg = totalFps / checkFrames;
                if (avg < 49)
                    detectedMods.Add($"Low FPS ({avg})");

                if (detectedMods.Count == 0 && ownedCosmetics.Count == 0)
                    return null;

                var result = player?.NickName;

                if (ownedCosmetics.Count > 0)
                    result += " is a " + string.Join(", ", ownedCosmetics);

                if (detectedMods.Count > 0)
                    result += ownedCosmetics.Count > 0
                        ? " and is using " + string.Join(", ", detectedMods)
                        : " is using " + string.Join(", ", detectedMods);

                return result;
            }
            catch
            {
                return null;
            }
        }

        public static VRRig? GetVRRigFromPlayer(NetPlayer? player)
        {
            try
            {
                return GorillaGameManager.instance.FindPlayerVRRig(player);
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        private class Wrapper;

        public abstract class ModData
        {
            public string? Name { get; set; }
            public string? Color { get; set; }
            public string? OriginalKey { get; set; }
        }
    }
}