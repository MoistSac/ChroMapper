﻿using System.Collections.Generic;
using LiteNetLib.Utils;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> conflictingData;

    public StrobeGeneratorGenerationAction() : base() { }

    public StrobeGeneratorGenerationAction(IEnumerable<BeatmapObject> generated, IEnumerable<BeatmapObject> conflicting)
        : base(generated) => conflictingData = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            DeleteObject(obj, false);
        foreach (var obj in conflictingData)
            SpawnObject(obj);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in conflictingData)
            DeleteObject(obj, false);
        foreach (var obj in Data)
            SpawnObject(obj);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, conflictingData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        conflictingData = DeserializeBeatmapObjectList(reader);
    }
}
