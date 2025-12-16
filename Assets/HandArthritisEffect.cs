using System.Collections.Generic;
using UnityEngine;

public class HandArthritisEffect : MonoBehaviour
{
    [Header("Tracking Source (real hand)")]
    public Transform trackingTarget;

    [Header("Root of visual hand rig")]
    public Transform visualRoot;

    Vector3 dampPos;
    Quaternion dampRot;

    struct PoseSample
    {
        public Vector3 pos;
        public Quaternion rot;
        public float time;
    }

    Queue<PoseSample> buffer = new Queue<PoseSample>();

    void Start()
    {
        dampPos = visualRoot.position;
        dampRot = visualRoot.rotation;
    }

    void LateUpdate()
    {
        var mgr = ArthritisManager.inst;
        if (mgr == null || trackingTarget == null || visualRoot == null)
            return;

        Vector3 rawPos = trackingTarget.position;
        Quaternion rawRot = trackingTarget.rotation;

        if (!mgr.arthritisOn)
        {
            buffer.Clear();
            visualRoot.position = rawPos;
            visualRoot.rotation = rawRot;
            dampPos = rawPos;
            dampRot = rawRot;
            return;
        }

        if (!mgr.useLatency)
        {
            // DAMPENING
            float t = Time.deltaTime * Mathf.Max(0.01f, mgr.dampStrength);
            dampPos = Vector3.Lerp(dampPos, rawPos, t);
            dampRot = Quaternion.Slerp(dampRot, rawRot, t);
            visualRoot.position = dampPos;
            visualRoot.rotation = dampRot;
        }
        else
        {
            // LATENCY
            float now = Time.time;
            float delay = mgr.latencySeconds;

            buffer.Enqueue(new PoseSample { pos = rawPos, rot = rawRot, time = now });

            while (buffer.Count > 0 && now - buffer.Peek().time > delay)
            {
                var s = buffer.Dequeue();
                dampPos = s.pos;
                dampRot = s.rot;
            }

            visualRoot.position = dampPos;
            visualRoot.rotation = dampRot;
        }
    }
}