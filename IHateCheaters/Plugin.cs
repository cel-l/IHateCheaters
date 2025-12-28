using System.Text;
using BepInEx;
using ExitGames.Client.Photon;
using IHateCheaters.Models;
using MonkeNotificationLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Debug;
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Local

namespace IHateCheaters;

[BepInPlugin("cel.ihatecheaters", Mod, Version)]
[BepInDependency("crafterbot.notificationlib")]
public class Plugin : BaseUnityPlugin
{
    public const string Mod = "IHateCheaters";
    public const string Version = "1.0.1";
    public const string Alias = "<color=#d91111>IHateCheaters</color>";

    private GunLib _gun = new()
    {
        ShouldFollow = true
    };

    public bool hasInitialized;
    public bool wasShooting;

    private void Start()
    {
        HarmonyPatches.ApplyHarmonyPatches();

        if (GorillaTagger.Instance != null)
            Initialize();
        else
            GorillaTagger.OnPlayerSpawned(Initialize);
    }

    private void Initialize()
    {
        try
        {
            gameObject.AddComponent<NetworkHandler>();
            gameObject.AddComponent<CheaterHandler>();
            gameObject.AddComponent<MiscHandler>();

            _gun = new GunLib();
            _gun.Start();
            hasInitialized = true;

            _ = CheckVersionAsync();
            _ = SetCustomPropertyAsync();
        }
        catch (Exception ex)
        {
            LogError($"[IHateCheaters] Failed to initialize: {ex}");
        }
    }

    private async Task SetCustomPropertyAsync()
    {
        using var request = new UnityWebRequest("https://api.aeris.now/custom-property", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();

        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var propertyValue = request.downloadHandler.text;

            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { propertyValue, true }
            });
        }
    }

    public async Task CheckVersionAsync()
    {
        var payload = JsonUtility.ToJson(new VersionRequest { mod_id = Mod, current_version = Version });
        var bodyRaw = Encoding.UTF8.GetBytes(payload);

        using var request = new UnityWebRequest("https://api.aeris.now/version", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var jsonString = request.downloadHandler.text;
            var json = JsonUtility.FromJson<VersionResponse>(jsonString);

            if (json.outdated)
                NotificationController.AppendMessage(Alias, $"Mod is outdated! Latest: {json.latest_version}", false, 10f);
        }
    }

    void LateUpdate()
    {
        if (!hasInitialized)
            return;

        _gun.LateUpdate();

        var gunChosenRig = _gun.ChosenRig;
        if (_gun.IsShooting && gunChosenRig)
        {
            if (!wasShooting)
            {
                if (gunChosenRig.OwningNetPlayer != null)
                {
                    NotificationController.AppendMessage(
                        Plugin.Alias,
                        $"Checking {GetColoredPlayerName(gunChosenRig.OwningNetPlayer)}",
                        false, 0.1f
                    );
                    _ = NetworkHandler.CheckPlayer(gunChosenRig.OwningNetPlayer, 0);
                }
            }

            wasShooting = true;
        }
        else
        {
            wasShooting = false;
        }
    }

    public static string? GetColoredPlayerName(NetPlayer? player)
    {
        var playerRig = CheaterHandler.GetVRRigFromPlayer(player);
        if (!playerRig)
            if (player != null)
                return player.NickName;
        if (!playerRig) return null;

        var playerColor = playerRig.playerColor;
        var hexColor = ColorUtility.ToHtmlStringRGB(playerColor);
        return $"<color=#{hexColor}>{player?.SanitizedNickName}</color>";
    }

    [Serializable]
    public class VersionRequest
    {
        public string? mod_id;
        public string? current_version;
    }

    [Serializable]
    private class VersionResponse(bool outdated, string latestVersion, string currentVersion, string message)
    {
        public bool outdated = outdated;
        public string latest_version = latestVersion;
        public string current_version = currentVersion;
        public string message = message;
    }
}