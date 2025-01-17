﻿using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;

/*
 * Seems weird? Let me explain.
 * 
 * In a nutshell, this is an Action that groups together multiple other Actions, which are mass undone/redone.
 * This is useful for storing many separate Actions that need to be grouped together, and not clog up the queue.
 */
public class ActionCollectionAction : BeatmapAction
{
    private IEnumerable<BeatmapAction> actions;
    private bool clearSelection;
    private bool forceRefreshesPool;

    public ActionCollectionAction() : base() { }

    public ActionCollectionAction(IEnumerable<BeatmapAction> beatmapActions, bool forceRefreshPool = false,
        bool clearsSelection = true, string comment = "No comment.")
        : base(beatmapActions.SelectMany(x => x.Data), comment)
    {
        foreach (var beatmapAction in beatmapActions)
            // Stops the actions wastefully refreshing the object pool
            beatmapAction.inCollection = true;

        actions = beatmapActions;
        clearSelection = clearsSelection;
        forceRefreshesPool = forceRefreshPool;
    }

    public override BeatmapObject DoesInvolveObject(BeatmapObject obj)
    {
        foreach (var action in actions)
        {
            var involvedObject = action.DoesInvolveObject(obj);

            if (involvedObject != null) return involvedObject;
        }

        return null;
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (clearSelection && !Networked) SelectionController.DeselectAll();

        foreach (var action in actions) action.Redo(param);

        if (forceRefreshesPool) RefreshPools(Data);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (clearSelection && !Networked) SelectionController.DeselectAll();

        foreach (var action in actions) action.Undo(param);

        if (forceRefreshesPool) RefreshPools(Data);
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(clearSelection);
        writer.Put(forceRefreshesPool);
        writer.Put(actions.Count());

        foreach (var action in actions)
        {
            writer.PutBeatmapAction(action);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        clearSelection = reader.GetBool();
        forceRefreshesPool = reader.GetBool();

        var count = reader.GetInt();
        var deserializedActions = new List<BeatmapAction>(count);

        for (var i = 0; i < count; i++)
        {
            var action = reader.GetBeatmapAction(Identity);
            action.inCollection = true;
            deserializedActions.Add(action);
        }

        actions = deserializedActions;
        Data = actions.Where(x => x != null && x.Data != null).SelectMany(x => x.Data);
    }
}
