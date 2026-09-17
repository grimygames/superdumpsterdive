using UnityEngine;

public interface IDumpsterMode
{
    void HandleItemClicked(string itemName, GameObject itemObject, TrashData data);
    void HandleItemTossed(TrashData data);
}