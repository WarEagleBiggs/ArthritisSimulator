using System.Collections.Generic;
using UnityEngine;

public class FingerDelay : MonoBehaviour
{
    // -------------------------
    // SHARED STATE (both hands)
    // -------------------------
    public static bool SharedEffectOn = true;

    [Header("Assign the wrist/root bone of the visual hand (e.g., L_Wrist / R_Wrist)")]
    public Transform visualWristRoot;

    [Header("Button Press Detection (no triggers)")]
    public Transform fingerTip;
    public Collider buttonCollider;
    public bool autoFindButtonByTag = true;
    public float insideEpsilon = 0.001f;

    [Header("Local override")]
    [Tooltip("If true, this instance uses SharedEffectOn. If false, uses effectOnLocal only.")]
    public bool syncWithOtherHand = true;

    [Tooltip("Only used when syncWithOtherHand = false.")]
    public bool effectOnLocal = true;

    [Header("Settings")]
    public bool useLatency = true;

    [Range(0f, 1f)]
    public float latencySeconds = 0.35f;

    public float dampStrength = 3f;
    public bool keepWristRealtime = true;

    struct Sample { public Quaternion rot; public float time; }

    private readonly List<Transform> bones = new List<Transform>();
    private readonly Dictionary<Transform, Queue<Sample>> buffers = new Dictionary<Transform, Queue<Sample>>();
    private readonly Dictionary<Transform, Quaternion> dampRot = new Dictionary<Transform, Quaternion>();

    private bool wasInsideButton = false;

    bool EffectOn
    {
        get => syncWithOtherHand ? SharedEffectOn : effectOnLocal;
        set
        {
            if (syncWithOtherHand) SharedEffectOn = value;
            else effectOnLocal = value;
        }
    }

    void Awake()
    {
        RebuildBoneList();
        TryAutoFindButton();
    }

    void OnEnable()
    {
        TryAutoFindButton();
        wasInsideButton = false;
    }

    void TryAutoFindButton()
    {
        if (buttonCollider) return;
        if (!autoFindButtonByTag) return;

        var go = GameObject.FindGameObjectWithTag("button-toggle");
        if (go) buttonCollider = go.GetComponent<Collider>();
    }

    [ContextMenu("Rebuild Bone List")]
    public void RebuildBoneList()
    {
        bones.Clear();
        buffers.Clear();
        dampRot.Clear();

        if (!visualWristRoot) return;

        var all = visualWristRoot.GetComponentsInChildren<Transform>(true);

        foreach (var t in all)
        {
            if (!t) continue;
            if (keepWristRealtime && t == visualWristRoot) continue;

            bones.Add(t);
            buffers[t] = new Queue<Sample>(64);
            dampRot[t] = t.rotation;
        }
    }

    void Update()
    {
        // Press detection (no triggers)
        if (!fingerTip) return;

        if (!buttonCollider) TryAutoFindButton();
        if (!buttonCollider) return;

        Vector3 p = fingerTip.position;

        Vector3 cp = buttonCollider.ClosestPoint(p);
        bool isInside = (cp - p).sqrMagnitude <= insideEpsilon * insideEpsilon;

        // Edge detect: outside -> inside toggles once
        if (isInside && !wasInsideButton)
        {
            EffectOn = !EffectOn; // toggles shared if syncWithOtherHand = true
        }

        wasInsideButton = isInside;
    }

    void LateUpdate()
    {
        if (!EffectOn || !visualWristRoot) return;

        float now = Time.time;

        foreach (var b in bones)
        {
            if (!b) continue;

            Quaternion trackedRot = b.rotation;

            if (useLatency)
            {
                var q = buffers[b];
                q.Enqueue(new Sample { rot = trackedRot, time = now });

                Quaternion applied = trackedRot;
                bool hasOld = false;

                while (q.Count > 0 && now - q.Peek().time > latencySeconds)
                {
                    applied = q.Dequeue().rot;
                    hasOld = true;
                }

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
