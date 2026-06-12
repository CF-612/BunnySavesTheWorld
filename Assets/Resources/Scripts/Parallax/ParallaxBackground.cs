using NUnit.Framework.Internal;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    private Camera mainCamera;
    private float lastCameraPosX;
    private float lastCameraPosY;
    private float cameraHalfWidth;
    [SerializeField] private ParallaxLayer[] backgroundLayers;

    private void Awake()
    {
        mainCamera = Camera.main;
        cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        InitializeLayers();
    }

    private void Start()
    {
        // 初始化上一次相机位置
        lastCameraPosX = mainCamera.transform.position.x;
        lastCameraPosY = mainCamera.transform.position.y;
    }

    private void Update()
    {
        float currentCameraPosX = mainCamera.transform.position.x;
        float currentCameraPosY = mainCamera.transform.position.y;

        float deltaX = currentCameraPosX - lastCameraPosX;
        float deltaY = currentCameraPosY - lastCameraPosY;

        lastCameraPosX = currentCameraPosX;
        lastCameraPosY = currentCameraPosY;

        float cameraLeftEdge = currentCameraPosX - cameraHalfWidth;
        float cameraRightEdge = currentCameraPosX + cameraHalfWidth;

        foreach (ParallaxLayer layer in backgroundLayers)
        {
            layer.Move(deltaX, deltaY);   // 传入X和Y的移动距离
            layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
        }
    }

    private void InitializeLayers()
    {
        foreach(ParallaxLayer layer in backgroundLayers)
            layer.CalculateImageWidth();
    }
}
