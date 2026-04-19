using UnityEngine;

public class Flock : MonoBehaviour
{
    public int fishIndex { get; private set; }

    public void Init(int index)
    {
        fishIndex = index;
    }
}