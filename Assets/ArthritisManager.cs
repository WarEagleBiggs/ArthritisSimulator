using UnityEngine;

public class ArthritisManager : MonoBehaviour
{
    public static ArthritisManager inst;

    [Header("Toggles")]
    public bool arthritisOn = false;   // master on/off
    public bool useLatency = false;    // false = dampening, true = latency

    [Header("Dampening Settings")]
    public float dampStrength = 3f;    // smaller => more sluggish

    [Header("Latency Settings")]
    public float latencySeconds = 0.4f; // delay in seconds

    void Awake()
    {
        if (inst != null && inst != this)
        {
            Destroy(gameObject);
            return;
        }
        inst = this;
    }
}