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

    private void updateLayersVertical()
    {
      // todo
    }

    #endregion
  }
}
