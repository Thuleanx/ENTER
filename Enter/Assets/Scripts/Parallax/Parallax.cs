using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Enter 
{
  public class Parallax : MonoBehaviour
  {
    public static Parallax Instance;
    
    [SerializeField] private Image _bgImageBackTop;
    [SerializeField] private Image _bgImageMidTop;
    [SerializeField] private Image _bgImageFrontTop;
    
    [SerializeField] private Image _bgImageBack;
    [SerializeField] private Image _bgImageMid;
    [SerializeField] private Image _bgImageFront;

    [SerializeField] private float _translateCorrectionValue; // an extra to apply after layer translation to make the two backgrounds seamless
    [SerializeField] private int _repeatFactor;

    private Camera _camera => Camera.main;
    private List<ParallaxItem> _layers; // background images

    private List<Vector3> _startPositions;
    private List<Vector2> _imageSizes;


    #region ================== Methods

    private void Awake()
    {
      if (Instance)
      {
        Destroy(gameObject);
        return;
      } 

      Instance = this;
      transform.SetParent(null);
      DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
      initialFrontTopOffset = frontTop.anchoredPosition.y;
      initialMidTopOffset   = midTop.anchoredPosition.y;
      initialBackTopOffset  = backTop.anchoredPosition.y;
      initialFrontOffset    = front.anchoredPosition.y;
      initialMidOffset      = mid.anchoredPosition.y;
      initialBackOffset     = back.anchoredPosition.y;
    }

    void LateUpdate()
    {
      updateLayersHorizontal();
      updateLayersVertical();
    }

    #endregion

    #region ================== Helpers

    private void updateLayersHorizontal()
    {
      float x = _camera.transform.position.x;
      Vector2 backOffset  = new Vector2(x / 1000, 0);
      Vector2 midOffset   = new Vector2(x / 500,  0);
      Vector2 frontOffset = new Vector2(x / 250,  0);

      _bgImageBackTop.material.mainTextureOffset  = backOffset;
      _bgImageMidTop.material.mainTextureOffset   = midOffset;
      _bgImageFrontTop.material.mainTextureOffset = frontOffset;

      _bgImageBack.material.mainTextureOffset  = backOffset;
      _bgImageMid.material.mainTextureOffset   = midOffset;
      _bgImageFront.material.mainTextureOffset = frontOffset;
    }

    private RectTransform frontTop => _bgImageFrontTop.GetComponent<RectTransform>();
    private RectTransform midTop   => _bgImageMidTop.GetComponent<RectTransform>();
    private RectTransform backTop  => _bgImageBackTop.GetComponent<RectTransform>();
    private RectTransform front    => _bgImageFront.GetComponent<RectTransform>();
    private RectTransform mid      => _bgImageMid.GetComponent<RectTransform>();
    private RectTransform back     => _bgImageBack.GetComponent<RectTransform>();

    private float initialFrontTopOffset;
    private float initialMidTopOffset;
    private float initialBackTopOffset;
    private float initialFrontOffset;
    private float initialMidOffset;
    private float initialBackOffset;
    
    private float x1 => frontTop.anchoredPosition.y - front.anchoredPosition.y;
    private float x2 => midTop.anchoredPosition.y - mid.anchoredPosition.y;
    private float x3 => backTop.anchoredPosition.y - back.anchoredPosition.y;
    private float h = 720;

    private float aMax = 29; // World space units of highest camera altitude
    private float aMin = 0;  // World space units of lowest camera altitude
    
    [field:SerializeField] private float a => _camera.transform.position.y;

    [field:SerializeField] private float x3Offset => -a / aMax * (x3 - h);
    [field:SerializeField] private float x2Offset => -a / aMax * (x2 - h);
    [field:SerializeField] private float x1Offset => -a / aMax * (x1 - h);

    private void updateLayersVertical()
    {
      Debug.Log("x3Offset" + x3Offset);
      Debug.Log("x2Offset" + x2Offset);
      Debug.Log("x1Offset" + x1Offset);

      frontTop.anchoredPosition = new Vector2(frontTop.anchoredPosition.x,  initialFrontTopOffset + x1Offset);
      midTop.anchoredPosition   = new Vector2(midTop.anchoredPosition.x,    initialMidTopOffset   + x2Offset);
      backTop.anchoredPosition  = new Vector2(backTop.anchoredPosition.x,   initialBackTopOffset  + x3Offset);
      front.anchoredPosition    = new Vector2(front.anchoredPosition.x,     initialFrontOffset    + x1Offset);
      mid.anchoredPosition      = new Vector2(mid.anchoredPosition.x,       initialMidOffset      + x2Offset);
      back.anchoredPosition     = new Vector2(back.anchoredPosition.x,      initialBackOffset     + x3Offset);

      // float cameraY = _camera.transform.position.y;
      // Vector2 backOffset  = new Vector2(0, cameraY / 3000);
      // Vector2 midOffset   = new Vector2(0, cameraY / 1500);
      // Vector2 frontOffset = new Vector2(0,  cameraY / 750);

      // _bgImageBackTop.material.mainTextureOffset  += backOffset;
      // _bgImageMidTop.material.mainTextureOffset   += midOffset;
      // _bgImageFrontTop.material.mainTextureOffset += frontOffset;

      // _bgImageBack.material.mainTextureOffset  += backOffset;
      // _bgImageMid.material.mainTextureOffset   += midOffset;
      // _bgImageFront.material.mainTextureOffset += frontOffset;

      // float x = _bgImageFrontTop.transform.position.x;
      // float y = _bgImageFrontTop.transform.position.y;
      // float z = _bgImageFrontTop.transform.position.z;
      // _bgImageFrontTop.transform.position = new Vector3(x, cameraY / 1000, z);
    }

    #endregion
  }
}