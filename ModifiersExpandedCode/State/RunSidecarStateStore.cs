using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace ModifiersExpanded.ModifiersExpandedCode.State;

public static class RunSidecarStateStore
{
    private const string SingleplayerSidecarFileName = "current_run.modifiers_expanded.save";
    private const string MultiplayerSidecarFileName = "current_run_mp.modifiers_expanded.save";

    private sealed class SidecarData
    {
        public int Version { get; set; } = 3;
        public float EnemyDamageMultiplier { get; set; } = 1.0f;
        public float EnemyHpMultiplier { get; set; } = 1.0f;
        public int EnemyAdditionalPlayers { get; set; }
        public float EasyPoolScalingPercent { get; set; } = 100.0f;
        public TimerState.UrgencySnapshot? UrgencySnapshot { get; set; }
    }

    public static void Save(bool isMultiplayer)
    {
        if (!SaveManager.Instance.IsProfileInitialized)
        {
            return;
        }

        try
        {
            var sidecarData = new SidecarData
            {
                EnemyDamageMultiplier = EnemyScalingState.Instance.DamageMultiplier,
                EnemyHpMultiplier = EnemyScalingState.Instance.HpMultiplier,
                EnemyAdditionalPlayers = EnemyScalingState.Instance.NumAdditionalPlayers,
                EasyPoolScalingPercent = EnemyScalingState.Instance.EasyPoolScalingPercent,
                UrgencySnapshot = TimerState.CaptureUrgencySnapshot(),
            };

            var payload = JsonSerializer.Serialize(sidecarData);
            CreateLocalSaveStore().WriteFile(GetSidecarPath(isMultiplayer), payload);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn(
                MainFile.CreateLogMessage($"Failed to save sidecar run state: {exception.Message}")
            );
        }
    }

    public static void Load(bool isMultiplayer, bool runSaveLoadSucceeded)
    {
        if (!runSaveLoadSucceeded || !SaveManager.Instance.IsProfileInitialized)
        {
            ResetPersistedState();
            return;
        }

        try
        {
            var saveStore = CreateLocalSaveStore();
            var sidecarPath = GetSidecarPath(isMultiplayer);
            if (!saveStore.FileExists(sidecarPath))
            {
                ResetPersistedState();
                return;
            }

            var payload = saveStore.ReadFile(sidecarPath);
            if (string.IsNullOrWhiteSpace(payload))
            {
                ResetPersistedState();
                return;
            }

            var sidecarData = JsonSerializer.Deserialize<SidecarData>(payload);
            if (sidecarData == null)
            {
                ResetPersistedState();
                return;
            }

            EnemyScalingState.Instance.DamageMultiplier = sidecarData.EnemyDamageMultiplier;
            EnemyScalingState.Instance.HpMultiplier = Math.Max(sidecarData.EnemyHpMultiplier, 1.0f);
            EnemyScalingState.Instance.NumAdditionalPlayers = sidecarData.EnemyAdditionalPlayers;
            EnemyScalingState.Instance.EasyPoolScalingPercent = Math.Clamp(
                sidecarData.EasyPoolScalingPercent,
                0.0f,
                100.0f
            );
            TimerState.SetPendingUrgencySnapshot(sidecarData.UrgencySnapshot);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn(
                MainFile.CreateLogMessage($"Failed to load sidecar run state: {exception.Message}")
            );
            ResetPersistedState();
        }
    }

    private static string GetSidecarPath(bool isMultiplayer)
    {
        return RunSaveManager.GetRunSavePath(
            SaveManager.Instance.CurrentProfileId,
            isMultiplayer ? MultiplayerSidecarFileName : SingleplayerSidecarFileName
        );
    }

    private static ISaveStore CreateLocalSaveStore()
    {
        return new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath(null));
    }

    private static void ResetPersistedState()
    {
        EnemyScalingState.Instance.Reset();
        TimerState.SetPendingUrgencySnapshot(null);
    }
}
