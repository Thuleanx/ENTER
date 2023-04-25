using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter 
{
    public class Parallax : MonoBehaviour
    {
        
        [SerializeField] private float _translateCorrectionValue; // an extra to apply after layer translation to make the two backgrounds seamless
        [SerializeField] private int _repeatFactor;

        private Camera _camera;        
        private List<ParallaxItem> _layers; // background images

        private List<Vector3> _startPositions;
        private List<Vector2> _imageSizes;


        #region ================== Methods

        void Start()
        {
            _startPositions = new List<Vector3>();
            _imageSizes = new List<Vector2>();
            _layers = new List<ParallaxItem>(GetComponentsInChildren<ParallaxItem>());

            foreach (ParallaxItem item in _layers) {
                InitializeLayerInfo(item);
            }

            RepeatLayers();
        }

        void FixedUpdate()
        {
            for (int i = 0; i < _layers.Count; i++) 
            {
                float layerOffset = ComputeCameraOffset(i);

                TranslateLayerPositionIfOffscreen(i, layerOffset);

                MoveLayer(i);
             }
        }

        #endregion



        #region ================== Helpers

        private void InitializeLayerInfo(ParallaxItem item)
        {
            _startPositions.Add(item.transform.position);
            _imageSizes.Add(item.GetComponentInChildren<SpriteRenderer>().bounds.size);
        }

        private void RepeatLayers()
        {
            List<ParallaxItem> repeatedLayers = new List<ParallaxItem>();
            for (int i = 0; i < _repeatFactor-1; i++) 
            {
                for (int j = 0; j < _layers.Count; j++)
                {
                    ParallaxItem item = _layers[j];
                    GameObject clonedLayer = Instantiate(
                        item.gameObject,
                        item.transform.position + Vector3.right*(i+1)*_imageSizes[j].x,
                        item.transform.rotation, 
                        transform
                    );
                    repeatedLayers.Add(clonedLayer.GetComponent<ParallaxItem>());
                    _startPositions.Add(clonedLayer.transform.position);
                }
                _imageSizes.AddRange(_imageSizes);
            }

            // Translate all layers backwards so that the camera is in the center of the repeated layers
            _layers.AddRange(repeatedLayers);
            for (int i = 0; i < _layers.Count; i++) 
            {
                float translationFactor = 0.5f*_imageSizes[i].x*(_repeatFactor-1);
                _layers[i].transform.position -= Vector3.right*translationFactor;
                _startPositions[i] = _layers[i].transform.position; 
            }
        }

        private float ComputeCameraOffset(int layerIndex)
        {
            float parallaxValue = _layers[layerIndex].parallaxValue;
            Vector2 imageLength = _imageSizes[layerIndex];

            Vector3 relativePos = _camera.transform.position * parallaxValue;   
            Vector3 dist = _camera.transform.position - relativePos;

            float cameraOffset = _camera.transform.position.x * (1-parallaxValue);
            return cameraOffset;
        }

        private void TranslateLayerPositionIfOffscreen(int layerIndex, float cameraOffset)
        {
            Vector2 imageLength = _imageSizes[layerIndex];
            float translationFactor = 0.5f*_imageSizes[layerIndex].x*(_repeatFactor-1);
            if (cameraOffset + 0.5f*translationFactor > _startPositions[layerIndex].x + imageLength.x)
            {
                _startPositions[layerIndex] += new Vector3(imageLength.x*_repeatFactor + _translateCorrectionValue, 0, 0);
            }
            if (cameraOffset + 0.5f*translationFactor < _startPositions[layerIndex].x - imageLength.x)
            {
                _startPositions[layerIndex] -= new Vector3(imageLength.x*_repeatFactor + _translateCorrectionValue, 0, 0);
            }
        }

        private void MoveLayer(int layerIndex)
        {
            Vector3 startPos = _startPositions[layerIndex];
            float parallaxValue = _layers[layerIndex].parallaxValue;

            Vector3 relativePos = _camera.transform.position * parallaxValue;
            relativePos.z = startPos.z;

            _layers[layerIndex].transform.position = startPos + relativePos;
        }


        #endregion
    }
}
