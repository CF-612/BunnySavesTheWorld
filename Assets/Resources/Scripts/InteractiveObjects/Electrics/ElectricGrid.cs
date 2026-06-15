using UnityEngine;
using System.Collections;

public class ElectricGrid : EntityEle
{
    [Header("电网独占设置（下沉效果）")]
    [Tooltip("完全下沉的距离（世界单位）")]
    [SerializeField] private float sinkDistance = 2f;
    
    [Tooltip("下沉动画持续时间（秒）")]
    [SerializeField] private float sinkDuration = 1f;
    
    [Tooltip("震动幅度（水平方向偏移）")]
    [SerializeField] private float vibrationAmplitude = 0.1f;
    
    [Tooltip("震动频率（Hz）")]
    [SerializeField] private float vibrationFrequency = 20f;

    private bool isSinking = false; // 防止重复触发

    public override void TurnOn()
    {
        base.TurnOn();
    }

    public override void TurnOff()
    {
        base.TurnOff();

        // 播放电网断裂音效
        AudioManager.Instance?.PlaySFX("Audio/SFX/InteractiveObjects/ElectricEffects/EleGridBroken");

        if (isSinking) return; // 已经在下沉，避免重复
        isSinking = true;

        StartCoroutine(SinkWithVibration());
    }

    private IEnumerator SinkWithVibration()
    {
        // 播放下沉音效
        AudioManager.Instance?.PlaySFX("Audio/SFX/InteractiveObjects/ElectricEffects/EleGridFall");

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.down * sinkDistance;
        float elapsed = 0f;

        while (elapsed < sinkDuration)
        {
            float t = elapsed / sinkDuration; // 0→1
            // 线性插值下沉
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            
            // 水平震动（基于正弦波）
            float vibrationOffset = Mathf.Sin(Time.time * vibrationFrequency) * vibrationAmplitude;
            newPos.x += vibrationOffset;
            
            transform.position = newPos;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终精确到达目标位置（并消除震动残留）
        transform.position = targetPos;
        
        // 完全下沉后删除物体
        Destroy(gameObject);
    }
}