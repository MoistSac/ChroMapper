﻿using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class RotationCallbackController : MonoBehaviour
{
    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;
    [FormerlySerializedAs("atsc")] public AudioTimeSyncController Atsc;
    [FormerlySerializedAs("events")][SerializeField] private EventGridContainer eventGrid;
    private readonly string[] enabledCharacteristics = { "360Degree", "90Degree", "Lawless" };

    public Action<bool, float> RotationChangedEvent; //Natural, degrees
    public bool IsActive { get; private set; }
    public BaseEvent LatestRotationEvent { get; private set; }

    public float Rotation { get; private set; }

    // Start is called before the first frame update
    internal void Start()
    {
        var set = BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet;
        IsActive = enabledCharacteristics.Contains(set.BeatmapCharacteristicName);
        if (IsActive && Settings.Instance.Reminder_Loading360Levels)
        {
            PersistentUI.Instance.ShowDialogBox(
                "PersistentUI", "360warning"
                , Handle360LevelReminder, PersistentUI.DialogBoxPresetType.OkIgnore);
        }

        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
        Atsc.PlayToggle += PlayToggle;
        Atsc.TimeChanged += OnTimeChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
        Atsc.PlayToggle -= PlayToggle;
        Atsc.TimeChanged -= OnTimeChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    private void UpdateRotateTrack(object obj)
    {
        if (Settings.Instance.RotateTrack) return;
        RotationChangedEvent?.Invoke(false, 0);
    }

    private void Handle360LevelReminder(int res) => Settings.Instance.Reminder_Loading360Levels = res == 0;

    private void OnTimeChanged()
    {
        if (Atsc.IsPlaying) return;
        PlayToggle(false);
    }

    private void PlayToggle(bool isPlaying)
    {
        if (!IsActive) return;
        var jsonTime = Atsc.CurrentJsonTime;

        var span = eventGrid.AllRotationEvents.AsSpan();
        var result = span.BinarySearchBy(jsonTime, e => e.JsonTime);
        var idx = result >= 0 ? result : ~result;

        // Continue marching forward until JsonTime extends beyond current time
        while (idx < span.Length && span[idx].JsonTime <= jsonTime) idx++;

        idx = Mathf.Min(idx, span.Length - 1);

        Rotation = 0;
        
        if (idx > 0)
        {
            for (var i = 0; i < idx; i++)
            {
                Rotation += span[i].GetRotationDegreeFromValue() ?? 0f;
            }

            LatestRotationEvent = span[idx];
        }
        else
        {
            LatestRotationEvent = null;
        }

        RotationChangedEvent.Invoke(false, Rotation);
    }

    private void EventPassedThreshold(bool initial, int index, BaseObject obj)
    {
        if (!IsActive) return;

        if (obj is not BaseEvent e) return;

        if (!e.IsLaneRotationEvent()) return;

        if (e == LatestRotationEvent) return;

        var rotationValue = e.GetRotationDegreeFromValue() ?? 0;
        Rotation += rotationValue;
        LatestRotationEvent = e;
        RotationChangedEvent.Invoke(true, Rotation);
    }
}
