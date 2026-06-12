using UnityEngine;

[System.Serializable]
public class ParallaxLayer 
{
    [SerializeField] private Transform background;
    [SerializeField] private float xparallaxMultiplier;
    [SerializeField] private float yParallaxMultiplier = 0.5f; // Y轴视差速度
    [SerializeField] private float imageWidthOffset = 10;
    private float imageFullWidth;
    private float imageHalfWidth;

    public void CalculateImageWidth()
    {
        imageFullWidth = background.GetComponent<SpriteRenderer>().bounds.size.x;
        imageHalfWidth = imageFullWidth / 2;
    }

    // 同时接收 X 和 Y 的相机移动距离
    public void Move(float deltaX, float deltaY)
    {
        float moveX = deltaX * xparallaxMultiplier;
        float moveY = deltaY * yParallaxMultiplier;
        background.position += new Vector3(moveX, moveY, 0);
    }

    public void LoopBackground(float cameraLeftEdge,float cameraRightEdge)
    {
        float imageRightEdge = (background.position.x + imageHalfWidth) - imageWidthOffset;
        float imageLeftEdge = (background.position.x - imageHalfWidth) + imageWidthOffset;

        if(imageRightEdge < cameraLeftEdge)
            background.position += Vector3.right * imageFullWidth;
        else if(imageLeftEdge > cameraRightEdge)
            background.position += Vector3.right * -imageFullWidth;
    }
}
