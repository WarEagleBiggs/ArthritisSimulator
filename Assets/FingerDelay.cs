using System.Collections.Generic;
using UnityEngine;

public class FingerDelay : MonoBehaviour
{
    [Header("Assign the wrist/root bone of the visual hand (e.g., L_Wrist / R_Wrist)")]
    public Transform visualWristRoot;

    [Header("Settings")]
    public bool effectOn = true;

    [Tooltip("True = visible delay. False = dampening.")]
    public bool useLatency = true;

    [Range(0f, 1f)]
    public float latencySeconds = 0.35f;

    [Tooltip("Only used when useLatency = false. Smaller = more sluggish.")]
    public float dampStrength = 3f;

    [Tooltip("Exclude wrist rotation (recommended).")]
    public bool keepWristRealtime = true;

    struct Sample { public Quaternion rot; public float time; }

    private readonly List<Transform> bones = new List<Transform>();
    private readonly Dictionary<Transform, Queue<Sample>> buffers = new Dictionary<Transform, Queue<Sample>>();
    private readonly Dictionary<Transform, Quaternion> dampRot = new Dictionary<Transform, Quaternion>();

    void Awake()
    {
        RebuildBoneList();
    }

    [ContextMenu("Rebuild Bone List")]
    public void RebuildBoneList()
    {
        bones.Clear();
        buffers.Clear();
        dampRot.Clear();

        if (!visualWristRoot) return;

        // Collect all transforms under wrist (including wrist)
        var all = visualWristRoot.GetComponentsInChildren<Transform>(true);

        foreach (var t in all)
        {
            if (!t) continue;
            if (keepWristRealtime && t == visualWristRoot) continue; // skip wrist

            bones.Add(t);
            buffers[t] = new Queue<Sample>(64);
            dampRot[t] = t.rotation;
        }
    }

    void LateUpdate()
    {
        if (!effectOn || !visualWristRoot) return;

        float now = Time.time;

        // IMPORTANT:
        // This script must run AFTER the system updates the hand visualizer,
        // so the current bone rotations represent the real tracked pose for this frame.
        foreach (var b in bones)
        {
            if (!b) continue;

            // current tracked pose (for this frame) is what we read right now
            Quaternion trackedRot = b.rotation;

            if (useLatency)
            {
                var q = buffers[b];
                q.Enqueue(new Sample { rot = trackedRot, time = now });

                // pop samples older than delay; the last popped becomes the applied delayed pose
                Quaternion applied = trackedRot;
                bool hasOld = false;

                while (q.Count > 0 && now - q.Peek().time > latencySeconds)
                {
                    applied = q.Dequeue().rot;
                    hasOld = true;
                }

                // If we don't have enough history yet, just follow tracking (prevents startup weirdness)
                b.rotation = hasOld ? applied : trackedRot;

                while (q.Count > 120) q.Dequeue();
            }
            else
            {
                float t = Time.deltaTime * Mathf.Max(0.01f, dampStrength);
                dampRot[b] = Quaternion.Slerp(dampRot[b], trackedRot, t);
                b.rotation = dampRot[b];
            }
        }
    }
}
