/*
* This file contains body pose detector pipeline and interface for Unity scene called "Body Pose"
* Main goal is to show how to use basic NN model like body pose inside Unity. It's using MoveNet body pose model.
*/

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;

namespace OAKForUnity
{
    public class DaiBodyPose : PredefinedBase
    {
        //Lets make our calls from the Plugin
        [DllImport("depthai-unity", CallingConvention = CallingConvention.Cdecl)]
        /*
        * Pipeline creation based on streams template
        *
        * @param config pipeline configuration 
        * @returns pipeline 
        */
        private static extern bool InitBodyPose(in PipelineConfig config);

        [DllImport("depthai-unity", CallingConvention = CallingConvention.Cdecl)]
        /*
        * Pipeline results
        *
        * @param frameInfo camera images pointers
        * @param getPreview True if color preview image is requested, False otherwise. Requires previewSize in pipeline creation.
        * @param width Unity preview image canvas width
        * @param height Unity preview image canvas height
        * @param useDepth True if depth information is requested, False otherwise. Requires confidenceThreshold in pipeline creation.
        * @param drawBodyPoseInPreview True to draw body landmakrs in the preview image
        * @param bodyLandmarkScoreThreshold Normalized score to filter body pose keypoints detections
        * @param retrieveInformation True if system information is requested, False otherwise. Requires rate in pipeline creation.
        * @param useIMU True if IMU information is requested, False otherwise. Requires freq in pipeline creation.
        * @param deviceNum Device selection on unity dropdown
        * @returns Json with results or information about device availability. 
        */
        private static extern IntPtr BodyPoseResults(out FrameInfo frameInfo, bool getPreview, int width, int height, bool useDepth, bool drawBodyPoseInPreview, float bodyLandmarkScoreThreshold, bool retrieveInformation, bool useIMU, int deviceNum);


        // Editor attributes
        [Header("RGB Camera")]
        public float cameraFPS = 30;
        public RGBResolution rgbResolution;
        private const bool Interleaved = true;
        private const ColorOrder ColorOrderV = ColorOrder.BGR;

        [Header("Mono Cameras")]
        public MonoResolution monoResolution;

        [Header("Body Pose Configuration")]
        public MedianFilter medianFilter;
        public bool useIMU = false;
        public bool retrieveSystemInformation = false;
        public bool drawBodyPoseInPreview;
        public float bodyLandmarkThreshold;
        private const bool GETPreview = true;
        private const bool UseDepth = true;

        [Header("Body Pose Results")]
        public Texture2D colorTexture;
        public string bodyPoseResults;
        public string systemInfo;
        public Vector3[] landmarks;

        public GameObject[] skeleton;
        public GameObject[] cylinders;
        public Vector2[] connections;


        //Initialize the important keypoints
        public GameObject shoulderleft;
        public GameObject elbowleft;
        public GameObject wristleft;


        //Initialize the variables needed to perform moving average
        public GameObject shoulderright1;
        public Vector3[] shoulderrights = new Vector3[30];
        public Vector3 averageshoulderright1 = new Vector3 (0.0f,0.0f,0.0f);

        public Vector3[] shoulderlefts = new Vector3[30];
        public Vector3 averageshoulderleft1 = new Vector3(0.0f, 0.0f, 0.0f);


        //Initialize rotation variables
        public Vector3 rightelbowrotation = new Vector3(0.0f, 0.0f, 0.0f);        
        public Vector3 rightwristrotation = new Vector3(0.0f, 0.0f, 0.0f);

        public Vector3 initialrightelbow;
        public Vector3 initialrightwrist;
        public Quaternion initialrightelbowrotate;
        public Quaternion initialrightshoulderrotate;


        //Initialize the same variables for th left arm
        public Vector3 leftelbowrotation = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 leftwristrotation = new Vector3(0.0f, 0.0f, 0.0f);

        public Vector3 initialleftelbow;
        public Vector3 initialleftwrist;
        public Quaternion initialleftelbowrotate;
        public Quaternion initialleftshoulderrotate;



        public GameObject elbowright1;
        public Vector3[] elbowrights = new Vector3[15];
        public Vector3 averageelbowright1 = new Vector3(0.0f, 0.0f, 0.0f);

        public GameObject wristright1;
        public Vector3[] wristrights = new Vector3[15];
        public Vector3 averagewristright1 = new Vector3(0.0f, 0.0f, 0.0f);

        //Unit Skeleton, for other purposes
        public GameObject unitskeleton1;
        public Vector3 skeleton_coordinate_diff1;


        public GameObject shoulderleft1;
        public GameObject elbowleft1;
        public GameObject wristleft1;
        public float elbowsize = 0.0f;

        public Vector3 elbowdisplacement;
        public Vector3 wristdisplacement;



        public GameObject elbowleftmarker;
        public GameObject shoulderleftmarker;
        public GameObject wristleftmarker;
        public GameObject unitskeleton;
        public Vector3 skeleton_coordinate_diff;



        // private attributes
        private Color32[] _colorPixel32;
        private GCHandle _colorPixelHandle;
        private IntPtr _colorPixelPtr;

        // Init textures. Each PredefinedBase implementation handles textures. Decoupled from external viz (Canvas, VFX, ...)
        void InitTexture()
        {
            colorTexture = new Texture2D(300, 300, TextureFormat.ARGB32, false);
            _colorPixel32 = colorTexture.GetPixels32();
            //Pin pixel32 array
            _colorPixelHandle = GCHandle.Alloc(_colorPixel32, GCHandleType.Pinned);
            //Get the pinned address
            _colorPixelPtr = _colorPixelHandle.AddrOfPinnedObject();
        }

        // Start. Init textures and frameInfo
        void Start()
        {
            landmarks = new Vector3[17];
            shoulderright1 = GameObject.Find("upperarm_r");
            elbowright1 = GameObject.Find("lowerarm_r");
            wristright1 = GameObject.Find("hand_r");
            elbowsize = (shoulderright1.transform.position - elbowright1.transform.position).magnitude;
            initialrightelbow = (elbowright1.transform.position - shoulderright1.transform.position);
            initialrightwrist = (wristright1.transform.position - elbowright1.transform.position);
            initialrightshoulderrotate = shoulderright1.transform.rotation;
            initialrightelbowrotate = wristright1.transform.rotation;


            for (int i =0; i<30; i++)
            {
                shoulderrights[i] = new Vector3(0.0f, 0.0f, 0.0f);
            }
            for (int i = 0; i < 30; i++)
            {
                shoulderlefts[i] = new Vector3(0.0f, 0.0f, 0.0f);
            }

            for (int i = 0; i < 15; i++)
            {
                elbowrights[i] = new Vector3(0.0f, 0.0f, 0.0f);
            }
            for (int i = 0; i < 15; i++)
            {
                wristrights[i] = new Vector3(0.0f, 0.0f, 0.0f);
            }


            shoulderleft1 = GameObject.Find("upperarm_l");
            elbowleft1 = GameObject.Find("lowerarm_l");
            wristleft1 = GameObject.Find("hand_l");

            initialleftelbow = (elbowleft1.transform.position - shoulderleft1.transform.position);
            initialleftwrist = (wristleft1.transform.position - elbowleft1.transform.position);
            initialleftshoulderrotate = shoulderleft1.transform.rotation;
            initialleftelbowrotate = wristleft1.transform.rotation;


            elbowdisplacement = new Vector3(0, 0, 0);
            wristdisplacement = new Vector3(0, 0, 0);


            unitskeleton1 = GameObject.Find("MyRiggedArms2");
            skeleton_coordinate_diff1 = unitskeleton1.transform.position - shoulderright1.transform.position;

            // Init dataPath to load body pose NN model
            _dataPath = Application.dataPath;

            InitTexture();

            // Init FrameInfo. Only need it in case memcpy data ptr on plugin lib.
            frameInfo.colorPreviewData = _colorPixelPtr;
        }

        // Prepare Pipeline Configuration and call pipeline init implementation
        protected override bool InitDevice()
        {
            // Color camera
            config.colorCameraFPS = cameraFPS;
            config.colorCameraResolution = (int)rgbResolution;
            config.colorCameraInterleaved = Interleaved;
            config.colorCameraColorOrder = (int)ColorOrderV;
            // Need it for color camera preview
            config.previewSizeHeight = 192; // 192 for lightning model, 256 for thunder model
            config.previewSizeWidth = 192;

            // Mono camera
            config.monoLCameraResolution = (int)monoResolution;
            config.monoRCameraResolution = (int)monoResolution;

            // Depth
            // Need it for depth
            config.confidenceThreshold = 230;
            config.leftRightCheck = true;
            config.ispScaleF1 = 2;
            config.ispScaleF2 = 3;
            config.manualFocus = 130;
            config.depthAlign = 1; // RGB align
            config.subpixel = true;
            config.deviceId = device.deviceId;
            config.deviceNum = (int)device.deviceNum;
            if (useIMU) config.freq = 400;
            if (retrieveSystemInformation) config.rate = 30.0f;
            config.medianFilter = (int)medianFilter;

            // Body Pose NN model
            config.nnPath1 = _dataPath +
                             "/Plugins/OAKForUnity/Models/movenet_singlepose_lightning_3.blob";

            // Plugin lib init pipeline implementation
            deviceRunning = InitBodyPose(config);

            // Check if was possible to init device with pipeline. Base class handles replay data if possible.
            if (!deviceRunning)
                Debug.LogError(
                    "Was not possible to initialrightize Body Pose. Check you have available devices on OAK For Unity -> Device Manager and check you setup correct deviceId if you setup one.");

            return deviceRunning;
        }

        // Get results from pipeline
        protected override void GetResults()
        {
            // if not doing replay
            if (!device.replayResults)
            {
                // Plugin lib pipeline results implementation
                bodyPoseResults = Marshal.PtrToStringAnsi(BodyPoseResults(out frameInfo, GETPreview, 300, 300, UseDepth, drawBodyPoseInPreview, bodyLandmarkThreshold, retrieveSystemInformation,
                    useIMU,
                    (int)device.deviceNum));
            }
            // if replay read results from file
            else
            {
                bodyPoseResults = device.results;
            }
        }

        void PlaceConnection(GameObject sp1, GameObject sp2, GameObject cyl)
        {
            Vector3 v3Start = sp1.transform.position;
            Vector3 v3End = sp2.transform.position;

            cyl.transform.position = (v3End - v3Start) / 2.0f + v3Start;

            Vector3 v3T = cyl.transform.localScale;
            v3T.y = (v3End - v3Start).magnitude / 2;

            cyl.transform.localScale = v3T;

            cyl.transform.rotation = Quaternion.FromToRotation(Vector3.up, v3End - v3Start);
        }

        // Process results from pipeline
        protected override void ProcessResults()
        {
            // If not replaying data
            if (!device.replayResults)
            {
                // Apply textures
                colorTexture.SetPixels32(_colorPixel32);
                colorTexture.Apply();
            }
            // if replaying data
            else
            {
                // Apply textures but get them from unity device implementation
                for (int i = 0; i < device.textureNames.Count; i++)
                {
                    if (device.textureNames[i] == "color")
                    {
                        colorTexture.SetPixels32(device.textures[i].GetPixels32());
                        colorTexture.Apply();
                    }
                }
            }

            if (string.IsNullOrEmpty(bodyPoseResults)) return;

            // EXAMPLE HOW TO PARSE INFO
            var json = JSON.Parse(bodyPoseResults)
            var count = 0;
            var arr = json["landmarks"];

            for (int i = 0; i < 17; i++) landmarks[i] = Vector3.zero;

            foreach (JSONNode obj in arr)
            {
                int index = -1;
                float x = 0.0f, y = 0.0f, z = 0.0f;
                float kx = 0.0f, ky = 0.0f;

                index = obj["index"];
                x = obj["location.x"];
                y = obj["location.y"];
                z = obj["location.z"];
                Vector3 curpos = new Vector3(x / 1000, y / 1000, z / 1000);

                if (obj["index"] == 5) //LEFT SHOULDER MOVENET - left arm of person but video is mirrored so right arm in video
                {




                    averageshoulderright1 = curpos;
                    for (int i =0;i <29; i++)
                    {
                        shoulderrights[i] =shoulderrights[i + 1];
                        averageshoulderright1 += shoulderrights[i];
                    }

                    shoulderrights[29] = curpos;
                    averageshoulderright1 += shoulderrights[29];
                    averageshoulderright1 /= 30;
                    shoulderright1.transform.position = averageshoulderright1; //RIGHT ARM OF 3d MODEL
               
                    unitskeleton1.transform.position = shoulderright1.transform.position + skeleton_coordinate_diff1;

                }

                if (obj["index"] == 6)//RIGHT SHOULDER MOVENET
                {
                    averageshoulderleft1 = curpos;
                    for (int i = 0; i < 29; i++)
                    {
                        shoulderlefts[i] = shoulderlefts[i + 1];
                        averageshoulderleft1 += shoulderlefts[i];
                    }

                    shoulderlefts[29] = curpos;
                    averageshoulderleft1 += shoulderlefts[29];
                    averageshoulderleft1 /= 30;


                    shoulderleft1.transform.position = averageshoulderleft1;

                }

                if (obj["index"] == 7) //LEFT ELBOW MOVENET
                {



                    averageelbowright1 = curpos;
                    for (int i = 0; i < 14; i++)
                    {
                        elbowrights[i] = elbowrights[i + 1];
                        averageelbowright1 += elbowrights[i];
                    }

                    elbowrights[14] = curpos;
                    averageelbowright1 += elbowrights[14];
                    averageelbowright1 /= 15;
                    curpos = averageelbowright1; //RIGHT ARM OF 3d MODEL



                    rightelbowrotation = Quaternion.FromToRotation(initialrightelbow, curpos - shoulderright1.transform.position).eulerAngles;

                    shoulderright1.transform.rotation = initialrightshoulderrotate;
                    shoulderright1.transform.Rotate(rightelbowrotation);



                    //elbowleft.transform.position = curpos;

                    elbowright1.transform.position = curpos;



                    elbowdisplacement = elbowright1.transform.position - shoulderright1.transform.position;
                    elbowdisplacement.x *= -1;

                    leftelbowrotation = Quaternion.FromToRotation(initialleftelbow, elbowdisplacement + shoulderleft1.transform.position).eulerAngles;

                    shoulderleft1.transform.rotation = initialleftshoulderrotate;
                    shoulderleft1.transform.Rotate(leftelbowrotation);

                    elbowleft1.transform.position = elbowdisplacement + shoulderleft1.transform.position;


                }

                if (obj["index"] == 9) //LEFT WRIST MOVENET
                {

                    averagewristright1 = curpos;
                    for (int i = 0; i < 14; i++)
                    {
                        wristrights[i] = wristrights[i + 1];
                        averagewristright1 += wristrights[i];
                    }

                    wristrights[14] = curpos;
                    averagewristright1 += wristrights[14];
                    averagewristright1 /= 15;
                    curpos= averagewristright1; //RIGHT ARM OF 3d MODEL




                    rightwristrotation = Quaternion.FromToRotation(initialrightwrist, curpos - elbowright1.transform.position).eulerAngles;

                    elbowright1.transform.rotation = initialrightelbowrotate;
                    elbowright1.transform.Rotate(rightwristrotation);


                    wristright1.transform.position = curpos;

                    wristdisplacement = wristright1.transform.position - elbowright1.transform.position;
                    wristdisplacement.x *= -1;

                    leftwristrotation = Quaternion.FromToRotation(initialleftwrist, wristdisplacement + elbowleft1.transform.position).eulerAngles;

                    elbowleft1.transform.rotation = initialleftelbowrotate;
                    elbowleft1.transform.Rotate(leftwristrotation);

                    wristleft1.transform.position = wristdisplacement + elbowleft1.transform.position;
                }
                count++;

                kx = obj["xpos"];
                ky = obj["ypos"];

                if (index != -1)
                {
                    landmarks[index] = new Vector3(x / 1000, y / 1000, z / 1000);
                    if (x != 0 && y != 0 && z != 0)
                    {
                        skeleton[index].SetActive(true);
                        skeleton[index].transform.position = landmarks[index];
                    }
                }
            }
            count = 0;

            bool allZero = true;
            for (int i = 0; i < 17; i++)
            {
                if (landmarks[i] != Vector3.zero)
                {
                    allZero = false;
                    break;
                }
            }

            // Update skeleton and movement
            if (!allZero)
            {
                for (int i = 0; i < 17; i++) if (landmarks[i] == Vector3.zero) skeleton[i].SetActive(false);

                // place dots connections
                for (int i = 0; i < 16; i++)
                {
                    int s = (int)connections[i].x;
                    int e = (int)connections[i].y;

                    if (landmarks[s] != Vector3.zero && landmarks[e] != Vector3.zero)
                    {
                        cylinders[i].SetActive(true);
                        PlaceConnection(skeleton[s], skeleton[e], cylinders[i]);
                    }
                    else cylinders[i].SetActive(false);
                }
            }

            if (!retrieveSystemInformation || json == null) return;

            float ddrUsed = json["sysinfo"]["ddr_used"];
            float ddrTotal = json["sysinfo"]["ddr_total"];
            float cmxUsed = json["sysinfo"]["cmx_used"];
            float cmxTotal = json["sysinfo"]["ddr_total"];
            float chipTempAvg = json["sysinfo"]["chip_temp_avg"];
            float cpuUsage = json["sysinfo"]["cpu_usage"];
            systemInfo = "Device System Information\nddr used: " + ddrUsed + "MiB ddr total: " + ddrTotal + " MiB\n" + "cmx used: " + cmxUsed + " MiB cmx total: " + cmxTotal + " MiB\n" + "chip temp avg: " + chipTempAvg + "\n" + "cpu usage: " + cpuUsage + " %";
        }
    }
}