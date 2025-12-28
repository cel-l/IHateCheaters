using MonkeNotificationLib;
using Photon.Pun;
using Photon.Realtime;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace IHateCheaters.Models
{
    internal class NetworkHandler : MonoBehaviourPunCallbacks
    {
        public static NetworkHandler? Instance { get; private set; }

        private static readonly HttpClient httpClient = new();
        private static readonly ConcurrentDictionary<string, bool> optOutCache = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this);
        }

        public override void OnJoinedRoom()
        {
            _ = CheckAllPlayers();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            _ = CheckPlayer(newPlayer, 5000);
        }

        public async Task CheckAllPlayers()
        {
            await Task.Delay(2000);

            var tasks = NetworkSystem.Instance.PlayerListOthers
                .Select(player => CheckPlayer(player))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        public static async Task CheckPlayer(NetPlayer? player, int initialDelay = 10000)
        {
            if (player == null)
                return;

            await Task.Delay(initialDelay);

            bool skip = await IsPlayerOptedOut(player.UserId);
            if (skip)
                return;

            var reason = await CheaterHandler.IsCheating(player);

            if (!string.IsNullOrEmpty(reason))
            {
                NotificationController.AppendMessage(Plugin.Alias, reason, false, 1f);
                AudioHandler.PlayNotification();

                GorillaTagger.Instance.StartVibration(false, 0.1f, GorillaTagger.Instance.tapHapticDuration);
                GorillaTagger.Instance.StartVibration(false, 0.1f, GorillaTagger.Instance.tapHapticDuration);
                await Task.Delay(200);
                GorillaTagger.Instance.StartVibration(true, 0.1f, GorillaTagger.Instance.tapHapticDuration);
                GorillaTagger.Instance.StartVibration(true, 0.1f, GorillaTagger.Instance.tapHapticDuration);
            }
        }

        private static async Task<bool> IsPlayerOptedOut(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (optOutCache.TryGetValue(userId, out bool cachedResult))
                return cachedResult;

            try
            {
                string payload = JsonConvert.SerializeObject(new { user_id = userId });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync("https://api.aeris.now/opted-out", content);
                response.EnsureSuccessStatusCode();

                string jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OptOutResponse>(jsonString);

                bool skip = result != null && result.skip;
                optOutCache[userId] = skip;
                return skip;
            }
            catch
            {
                optOutCache[userId] = false;
                return false;
            }
        }

        private class OptOutResponse
        {
            public bool skip { get; set; }
        }
    }
}