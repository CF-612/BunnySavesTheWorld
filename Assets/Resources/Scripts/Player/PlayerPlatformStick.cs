using UnityEngine;

public class PlayerPlatformStick : MonoBehaviour
{
    private Transform originalParent; // 玩家原始父物体

    void Start()
    {
        originalParent = transform.parent; // 记录玩家当前的原始父物体
    }

    void OnCollisionEnter2D(Collision2D collision) //当玩家触碰到碰撞体时触发
    {
        if (collision.gameObject.CompareTag("MovingPlatform")) // 如果带有 "MovingPlatform" tag标签
        {
            transform.SetParent(collision.transform); // 将玩家的父物体设置为平台，这样玩家就会跟随平台移动
        }
    }

    void OnCollisionExit2D(Collision2D collision) //当玩家离开碰撞体时触发
    {
        if (collision.gameObject.CompareTag("MovingPlatform")) // 如果带有 "MovingPlatform" tag标签
        {
            transform.SetParent(originalParent); // 将玩家的父物体重置为原始父物体，这样玩家就不会再跟随平台移动
        }
    }
}