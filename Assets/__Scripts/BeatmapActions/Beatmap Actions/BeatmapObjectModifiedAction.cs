﻿using LiteNetLib.Utils;

public class BeatmapObjectModifiedAction : BeatmapAction
{
    private readonly bool addToSelection;

    private BeatmapObject editedData;
    private BeatmapObject editedObject;
    private BeatmapObject originalData;
    private BeatmapObject originalObject;

    public BeatmapObjectModifiedAction() : base() { }

    public BeatmapObjectModifiedAction(BeatmapObject edited, BeatmapObject originalObject, BeatmapObject originalData,
        string comment = "No comment.", bool keepSelection = false) : base(new[] { edited, originalObject }, comment)
    {
        editedObject = edited;
        editedData = BeatmapObject.GenerateCopy(edited);

        this.originalData = originalData;
        this.originalObject = originalObject;
        addToSelection = keepSelection;
    }

    public override BeatmapObject DoesInvolveObject(BeatmapObject obj) => obj == editedObject ? originalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.Time.CompareTo(originalData.Time) != 0)
        {
            DeleteObject(editedObject, false);
            SelectionController.Deselect(editedObject, false);

            originalObject.Apply(originalData);
            SpawnObject(originalObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet
            originalObject.Apply(originalData);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked)
        {
            SelectionController.Select(originalObject, addToSelection, true, !inCollection);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.Time.CompareTo(originalData.Time) != 0)
        {
            DeleteObject(originalObject, false);
            SelectionController.Deselect(originalObject, false);

            editedObject.Apply(editedData);
            SpawnObject(editedObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet 
            editedObject.Apply(editedData);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked)
        {
            SelectionController.Select(editedObject, addToSelection, true, !inCollection);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(editedData);
        writer.PutBeatmapObject(originalData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        editedData = reader.GetBeatmapObject();
        editedObject = BeatmapObject.GenerateCopy(editedData);
        originalData = reader.GetBeatmapObject();
        originalObject = BeatmapObject.GenerateCopy(originalData);

        Data = new[] { editedObject, originalObject };
    }
}
