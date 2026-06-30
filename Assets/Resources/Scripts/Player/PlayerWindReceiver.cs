using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家风力接收器。由 OnTriggerEnter2D/OnTriggerExit2D 维护风区列表，
/// 各状态按需调用 GetWindVelocity() 获取当前合成风速。
/// 同一帧内多次调用返回缓存值，避免重复计算。
/// </summary>
public class PlayerWindReceiver : MonoBehaviour
{
    private readonly List<WindZoneData> activeWindZones = new List<WindZoneData>();

    /// <summary>延迟移除：风区 → 退出时间。避免边界进出振荡。</summary>
    private readonly Dictionary<WindZoneData, float> pendingRemovals = new Dictionary<WindZoneData, float>();
    private readonly List<WindZoneData> removalList = new List<WindZoneData>(); // 复用，避免 GC
    private const float ExitGracePeriod = 0.15f;

    public bool IsBeingBlown => activeWindZones.Count > 0;

    private Transform cachedTransform;

    // 按帧缓存
    private int lastCalcFrame = -1;
    private Vector2 cachedWindVelocity;
    private WindZoneData cachedStrongestZone;

    private void Awake()
    {
        cachedTransform = transform;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }
    }

    // ——— Trigger 检测 ———

    private void OnTriggerEnter2D(Collider2D other)
    {
        WindZoneData windZone = other.GetComponentInParent<WindZoneData>();
        if (windZone == null || !windZone.isActiveAndEnabled)
            return;

        // 取消该风区的延迟移除
        pendingRemovals.Remove(windZone);

        if (!activeWindZones.Contains(windZone))
        {
            activeWindZones.Add(windZone);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        WindZoneData windZone = other.GetComponentInParent<WindZoneData>();
        if (windZone == null)
            return;

        // 不立即移除，记录退出时间，延迟处理
        if (!pendingRemovals.ContainsKey(windZone))
        {
            pendingRemovals[windZone] = Time.time;
        }
    }

    // ——— 公开接口 ———

    /// <summary>获取当前外部风速向量（同一帧内多次调用返回缓存）</summary>
    public Vector2 GetWindVelocity()
    {
        EnsureCalculated();
        return cachedWindVelocity;
    }

    /// <summary>获取当前影响力最大的风区（同一帧内多次调用返回缓存）</summary>
    public WindZoneData GetStrongestZone()
    {
        EnsureCalculated();
        return cachedStrongestZone;
    }

    // ——— 内部计算 ———

    private void EnsureCalculated()
    {
        if (lastCalcFrame == Time.frameCount)
            return;

        lastCalcFrame = Time.frameCount;

        // 处理到期延迟移除
        ProcessPendingRemovals();

        if (activeWindZones.Count == 0)
        {
            cachedWindVelocity = Vector2.zero;
            cachedStrongestZone = null;
            return;
        }

        Vector2 strongestWind = Vector2.zero;
        float maxForceSqr = 0f;
        WindZoneData strongest = null;
        Vector2 playerPos = cachedTransform.position;

        foreach (var zone in activeWindZones)
        {
            Vector2 windForce = zone.GetWindForceAt(playerPos);
            float sqrMag = windForce.sqrMagnitude;
            if (sqrMag > maxForceSqr)
            {
                maxForceSqr = sqrMag;
                strongestWind = windForce;
                strongest = zone;
            }
        }

        cachedWindVelocity = strongestWind;
        cachedStrongestZone = strongest;
    }

    private void ProcessPendingRemovals()
    {
        if (pendingRemovals.Count == 0)
            return;

        float now = Time.time;
        removalList.Clear();
        foreach (var kvp in pendingRemovals)
        {
            if (now - kvp.Value >= ExitGracePeriod)
                removalList.Add(kvp.Key);
        }
        foreach (var zone in removalList)
        {
            activeWindZones.Remove(zone);
            pendingRemovals.Remove(zone);
        }
    }
}
