using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WireManager : MonoBehaviour
{
    public enum WireFixMode
    {
        FreeHanging,
        FixedBothEnds
    }

    [Header("生成模式与配置")]
    [Tooltip("FreeHanging: 自由垂落; FixedBothEnds: 两端固定")]
    public WireFixMode fixMode = WireFixMode.FreeHanging;
    [Tooltip("FixedBothEnds 模式下的尾部固定锚点（可以是场景中任意一个空物体）")]
    public Transform endAnchor;

    public GameObject wirePrefab;
    public int segmentCount = 5;
    [Tooltip("期望的物理间距。两端固定时，系统会计算总长并自动微调Y轴缩放防挤压")]
    public float segmentSpacing = 0.5f;
    [Tooltip("电线预制体的默认物理长度（碰撞体长度）。用于动态计算Y轴缩放倍数")]
    public float baseSegmentLength = 1f;

    [Header("平滑视觉配置")]
    public Material wireMaterial;
    public float wireWidth = 0.2f;
    [Range(1, 10)]
    public int smoothingSegments = 3;
    public float topExtension = 0f;
    public float bottomExtension = 0.25f;

    [Header("渲染层级")]
    [Tooltip("LineRenderer 所在的 Sorting Layer 名称")]
    public string sortingLayerName = "Ground";
    [Tooltip("LineRenderer 在 Sorting Layer 中的排序值")]
    public int sortingOrder = -1;

    [Header("断裂清理设置")]
    [Tooltip("完全脱落、不挂靠任何锚点的游离电线，会在多少秒后自动渐隐并消失")]
    public float disappearDelay = 3f;

    [Header("联动配置")]
    public UnityEvent onChainBroken;

    private bool hasBroken = false;
    
    private class WireChain
    {
        public LineRenderer line;
        public List<Transform> nodes;
    }
    
    private List<WireChain> activeChains = new List<WireChain>();

    private void Start()
    {
        BindEvents();
        SetupVisuals();
    }

    private void BindEvents()
    {
        Wire[] wires = GetComponentsInChildren<Wire>();
        foreach (Wire w in wires)
        {
            w.onWireBroken.AddListener(OnAnyWireBroken);
        }
    }

    private void SetupVisuals()
    {
        Wire[] wires = GetComponentsInChildren<Wire>();
        if (wires.Length == 0) return;

        List<Transform> initialNodes = new List<Transform>();
        
        initialNodes.Add(this.transform);
        
        foreach (Wire w in wires)
        {
            initialNodes.Add(w.transform);
        }

        if (fixMode == WireFixMode.FixedBothEnds && endAnchor != null)
        {
            initialNodes.Add(endAnchor);
        }

        CreateNewChain(initialNodes);
    }

    private void CreateNewChain(List<Transform> nodes)
    {
        GameObject lrObj = new GameObject("WireRenderer");
        lrObj.transform.SetParent(this.transform);
        
        LineRenderer lr = lrObj.AddComponent<LineRenderer>();
        lr.material = wireMaterial;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.textureMode = LineTextureMode.Tile; 
        lr.useWorldSpace = true;
        lr.positionCount = 0;

        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        WireChain newChain = new WireChain { line = lr, nodes = nodes };
        activeChains.Add(newChain);

        bool isAnchored = false;
        if (nodes.Count > 0)
        {
            if (nodes[0] == this.transform) 
                isAnchored = true;
                
            if (fixMode == WireFixMode.FixedBothEnds && endAnchor != null && nodes[nodes.Count - 1] == endAnchor) 
                isAnchored = true;
        }

        if (!isAnchored)
        {
            StartCoroutine(CleanupDetachedChain(newChain, disappearDelay));
        }
    }

    private void LateUpdate()
    {
        activeChains.RemoveAll(c => c.line == null);

        for (int i = activeChains.Count - 1; i >= 0; i--)
        {
            WireChain chain = activeChains[i];
            
            chain.nodes.RemoveAll(n => n == null);

            CheckForSplits(chain, i);
            UpdateLineRenderer(chain);
        }
    }

    private void CheckForSplits(WireChain chain, int chainIndex)
    {
        for (int i = 1; i < chain.nodes.Count; i++)
        {
            Transform node = chain.nodes[i];
            
            if (fixMode == WireFixMode.FixedBothEnds && node == endAnchor) 
                continue;

            HingeJoint2D hinge = node.GetComponent<HingeJoint2D>();
            
            if (hinge != null && !hinge.enabled)
            {
                List<Transform> newChainNodes = chain.nodes.GetRange(i, chain.nodes.Count - i);
                chain.nodes.RemoveRange(i, chain.nodes.Count - i);

                CreateNewChain(newChainNodes);
                break;
            }
        }
    }

    private void UpdateLineRenderer(WireChain chain)
    {
        if (chain.nodes.Count == 0)
        {
            chain.line.positionCount = 0;
            return;
        }

        if (chain.nodes.Count == 1)
        {
            chain.line.positionCount = 2;
            Transform singleNode = chain.nodes[0];
            
            float halfSpacing = segmentSpacing / 2f;
            Vector3 topPos = singleNode.position + singleNode.up * (halfSpacing + topExtension);
            Vector3 bottomPos = singleNode.position - singleNode.up * (halfSpacing + bottomExtension);
            
            chain.line.SetPosition(0, topPos);
            chain.line.SetPosition(1, bottomPos);
            return;
        }

        List<Vector3> smoothPoints = new List<Vector3>();

        for (int i = 0; i < chain.nodes.Count - 1; i++)
        {
            Vector3 p0 = chain.nodes[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = chain.nodes[i].position;
            Vector3 p2 = chain.nodes[i + 1].position;
            Vector3 p3 = chain.nodes[Mathf.Min(i + 2, chain.nodes.Count - 1)].position;

            int segments = (i == chain.nodes.Count - 2) ? smoothingSegments : smoothingSegments - 1;
            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)smoothingSegments;
                smoothPoints.Add(GetCatmullRomPosition(t, p0, p1, p2, p3));
            }
        }

        if (smoothPoints.Count >= 2)
        {
            if (topExtension > 0.001f)
            {
                Vector3 startDir = (smoothPoints[0] - smoothPoints[1]).normalized;
                smoothPoints[0] = smoothPoints[0] + startDir * topExtension;
            }

            if (bottomExtension > 0.001f)
            {
                int lastIdx = smoothPoints.Count - 1;
                Vector3 endDir = (smoothPoints[lastIdx] - smoothPoints[lastIdx - 1]).normalized;
                smoothPoints[lastIdx] = smoothPoints[lastIdx] + endDir * bottomExtension;
            }
        }

        chain.line.positionCount = smoothPoints.Count;
        chain.line.SetPositions(smoothPoints.ToArray());
    }

    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;
        return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
    }

    private IEnumerator CleanupDetachedChain(WireChain chain, float delay)
    {
        yield return new WaitForSeconds(delay);

        float fadeTime = 0.5f;
        float elapsed = 0f;
        
        if (chain.line != null)
        {
            Color startColor = chain.line.startColor;
            while (elapsed < fadeTime)
            {
                if (chain.line == null) break;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeTime);
                Color c = new Color(startColor.r, startColor.g, startColor.b, alpha);
                chain.line.startColor = c;
                chain.line.endColor = c;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (chain.nodes != null)
        {
            foreach (var node in chain.nodes)
            {
                if (node != null && node != this.transform && node != endAnchor) 
                {
                    Destroy(node.gameObject);
                }
            }
        }

        if (chain.line != null) Destroy(chain.line.gameObject);
    }

    private void OnAnyWireBroken()
    {
        if (hasBroken) return;
        
        hasBroken = true;
        onChainBroken?.Invoke();
    }

    // 核心算法：生成物理完美的 V字型 下垂轨迹，绝不折叠
    private Vector3[] CalculateVShapePositions(out float actualSpacing)
    {
        Vector3[] pos = new Vector3[segmentCount];
        Vector3 startPos = transform.position;
        actualSpacing = segmentSpacing;

        if (fixMode == WireFixMode.FreeHanging || endAnchor == null)
        {
            for (int i = 0; i < segmentCount; i++)
            {
                pos[i] = startPos + Vector3.down * (segmentSpacing * (i + 1));
            }
            return pos;
        }

        Vector3 endPos = endAnchor.position;
        float straightDist = Vector3.Distance(startPos, endPos);
        float expectedLength = (segmentCount + 1) * segmentSpacing;

        // 保底限制：确保电线总长至少略长于直线距离，绝不能紧绷报错
        float actualLength = Mathf.Max(expectedLength, straightDist + 0.1f);
        actualSpacing = actualLength / (segmentCount + 1);

        // 寻找 V型 顶点
        Vector3 midPoint = (startPos + endPos) / 2f;
        Vector3 diff = endPos - startPos;

        // 获取连线的垂直法向量，并强制要求朝下，产生真实的重力下坠感
        Vector3 perp = new Vector3(diff.y, -diff.x, 0).normalized;
        if (perp.y > 0)
        {
            perp = -perp; 
        }

        // 勾股定理算出 V型 底部的下垂高度 h
        float halfActual = actualLength / 2f;
        float halfStraight = straightDist / 2f;
        float h = Mathf.Sqrt(Mathf.Max(0, halfActual * halfActual - halfStraight * halfStraight));

        Vector3 cornerPos = midPoint + perp * h;
        float d1 = actualLength / 2f;

        // 按真实间距串联分配
        for (int i = 0; i < segmentCount; i++)
        {
            float distAlongPath = actualSpacing * (i + 1);

            if (distAlongPath <= d1)
            {
                Vector3 dir = (cornerPos - startPos).normalized;
                if (dir == Vector3.zero) dir = Vector3.down;
                pos[i] = startPos + dir * distAlongPath;
            }
            else
            {
                Vector3 dir = (endPos - cornerPos).normalized;
                if (dir == Vector3.zero) dir = Vector3.down;
                pos[i] = cornerPos + dir * (distAlongPath - d1);
            }
        }

        return pos;
    }

    [ContextMenu("生成物理电线链条")]
    public void GenerateWires()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("无法在游戏运行期间重构物理链条，请在非运行状态的编辑模式下执行。");
            return;
        }

        if (wirePrefab == null) return;

        if (wirePrefab.GetComponent<Rigidbody2D>() == null || wirePrefab.GetComponent<HingeJoint2D>() == null)
        {
            Debug.LogWarning("电线预制体缺少 Rigidbody2D 或 HingeJoint2D 组件！");
            return;
        }

        if (fixMode == WireFixMode.FixedBothEnds && endAnchor == null)
        {
            Debug.LogError("当前生成模式为两端固定(FixedBothEnds)，但未指定尾部锚点(End Anchor)，生成中止。");
            return;
        }

        Wire[] existingWires = GetComponentsInChildren<Wire>();
        foreach (Wire w in existingWires)
        {
            DestroyImmediate(w.gameObject);
        }

        float actualSpacing;
        Vector3[] targetPositions = CalculateVShapePositions(out actualSpacing);

        Rigidbody2D parentRb = GetComponent<Rigidbody2D>();
        Rigidbody2D previousRb = parentRb;
        Vector3 currPos = transform.position;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = Instantiate(wirePrefab, transform);
            segment.name = "WireSegment_" + i;
            
            Vector3 targetPos = targetPositions[i];
            segment.transform.position = targetPos;

            Vector3 dirToPrev = (currPos - targetPos).normalized;
            if (dirToPrev != Vector3.zero)
            {
                segment.transform.up = dirToPrev;
            }

            // 通过计算目标间距与预制体基础长度的比例，直接修改物体的 Y轴缩放
            if (baseSegmentLength > 0.01f)
            {
                float scaleY = actualSpacing / baseSegmentLength;
                Vector3 origScale = segment.transform.localScale;
                segment.transform.localScale = new Vector3(origScale.x, scaleY, origScale.z);
            }

            HingeJoint2D hinge = segment.GetComponent<HingeJoint2D>();
            Rigidbody2D currentRb = segment.GetComponent<Rigidbody2D>();
            
            // 因为物体整体被缩放了，局部坐标系下的锚点位置应继续使用预制体的原始基础长度
            hinge.anchor = new Vector2(0, baseSegmentLength / 2f);
            hinge.connectedBody = previousRb;
            hinge.autoConfigureConnectedAnchor = true;

            previousRb = currentRb;
            currPos = targetPos;

            if (i == segmentCount - 1 && fixMode == WireFixMode.FixedBothEnds && endAnchor != null)
            {
                Rigidbody2D anchorRb = endAnchor.GetComponent<Rigidbody2D>();
                if (anchorRb == null)
                {
                    anchorRb = endAnchor.gameObject.AddComponent<Rigidbody2D>();
                    anchorRb.bodyType = RigidbodyType2D.Static;
                }
                
                HingeJoint2D tailHinge = segment.AddComponent<HingeJoint2D>();
                tailHinge.connectedBody = anchorRb;
                // 尾部锚点同样使用预制体的原始基础长度
                tailHinge.anchor = new Vector2(0, -baseSegmentLength / 2f);
                tailHinge.autoConfigureConnectedAnchor = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        
        float tempSpacing;
        Vector3[] positions = CalculateVShapePositions(out tempSpacing);
        
        if (positions == null || positions.Length == 0) return;

        Vector3 prevPos = transform.position;
        for (int i = 0; i < positions.Length; i++)
        {
            Gizmos.DrawLine(prevPos, positions[i]);
            Gizmos.DrawWireSphere(positions[i], wireWidth * 0.5f);
            prevPos = positions[i];
        }

        if (fixMode == WireFixMode.FixedBothEnds && endAnchor != null)
        {
            Gizmos.DrawLine(prevPos, endAnchor.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(endAnchor.position, wireWidth * 0.5f);
        }
    }
}