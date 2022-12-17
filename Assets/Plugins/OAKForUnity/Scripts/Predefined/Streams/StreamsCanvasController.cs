/*
 * Canvas controller goal is decouple UI from pipeline (PredefinedBase) and unity device (OAKDevice)
 * In order to give some flexibility we're using Material as basic object with Texture2D
 * In that way we can use Image in Canvas UI or just Material in objects
 */

using UnityEngine;
using UnityEngine.UI;

namespace OAKForUnity
{
    public class StreamsCanvasController : MonoBehaviour
    {
        // public attributes
        
        // Pipeline for texture binding
        public DaiStreams pipeline;
        
        [Header("UI Binding")] 
        public Image colorCameraImage;
        public Image monoRCameraImage;
        public Image monoLCameraImage;
        public Image disparityImage;
        public Image depthCameraImage;

        private bool _init = false;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Binding Textures. Wait to have pipeline running.
        private void Init()
        {
            // Texture2D binding
            colorCameraImage.material.mainTexture = pipeline.colorTexture;
            monoRCameraImage.material.mainTexture = pipeline.monoRTexture;
            monoLCameraImage.material.mainTexture = pipeline.monoLTexture;
            disparityImage.material.mainTexture = pipeline.disparityTexture;
            depthCameraImage.material.mainTexture = pipeline.depthTexture;
            _init = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (pipeline.deviceRunning && !_init) Init();
        }
    }
}