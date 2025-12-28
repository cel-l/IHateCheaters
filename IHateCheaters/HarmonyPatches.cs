using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace IHateCheaters;

internal static class HarmonyPatches
{
    private static Harmony? _instance;
    private static bool IsPatched { get; set; }
    private const string InstanceId = "cel.ihatecheaters";

    internal static void ApplyHarmonyPatches()
    {
        if (IsPatched) return;

        _instance ??= new Harmony(InstanceId);

        try
        {
            _instance.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IHateCheaters] Harmony patching failed: {ex}");
        }

        IsPatched = true;
    }

    internal static void RemoveHarmonyPatches()
    {
        if (_instance is null || !IsPatched) return;

        try
        {
            _instance.UnpatchSelf();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IHateCheaters] Failed to remove Harmony patches: {ex}");
        }

        IsPatched = false;
    }
}