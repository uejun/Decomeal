using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using OpenCVForUnityExample;


public class HandDetection : MonoBehaviour {

    /// <summary>
    /// Set the name of the device to use.
    /// </summary>
    [SerializeField, TooltipAttribute("Set the name of the device to use.")]
    public string requestedDeviceName = null;

    /// <summary>
    /// Set the width of WebCamTexture.
    /// </summary>
    //[SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
    //public int requestedWidth = 640;
    public int requestedWidth = Screen.width;

    /// <summary>
    /// Set the height of WebCamTexture.
    /// </summary>
    //[SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
    //public int requestedHeight = 480;
    public int requestedHeight = Screen.height;

    /// <summary>
    /// Set FPS of WebCamTexture.
    /// </summary>
    [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
    public int requestedFPS = 30;

    /// <summary>
    /// Set whether to use the front facing camera.
    /// </summary>
    [SerializeField, TooltipAttribute("Set whether to use the front facing camera.")]
    public bool requestedIsFrontFacing = false;

    /// <summary>
    /// The webcam texture.
    /// </summary>
    WebCamTexture webCamTexture;

    /// <summary>
    /// The webcam device.
    /// </summary>
    WebCamDevice webCamDevice;

    /// <summary>
    /// The rgba mat.
    /// </summary>
    Mat rgbaMat;

    /// <summary>
    /// The colors.
    /// </summary>
    Color32[] colors;

    /// <summary>
    /// The texture.
    /// </summary>
    Texture2D texture;

    /// <summary>
    /// Indicates whether this instance is waiting for initialization to complete.
    /// </summary>
    bool isInitWaiting = false;

    /// <summary>
    /// Indicates whether this instance has been initialized.
    /// </summary>
    bool hasInitDone = false;

    /// <summary>
    /// The FPS monitor.
    /// </summary>
    FpsMonitor fpsMonitor;

    // 出力用
    public GameObject outputCamera1; // 変換後の画像を撮影するためのカメラ. インスペクタで設定してください
    OutputCamQuad outputScreenQuad; // outputCamera1が撮影するQuad. 変換後の画像を投影する

    Mat mat;

    void Start()
    {
        InitializeCV();

        // 出力画面 初期化
        outputScreenQuad = outputCamera1.GetComponent<OutputCamQuad>();

    }

    /// <summary>
    /// Initializes webcam texture.
    /// </summary>
    private void InitializeCV()
    {
        if (isInitWaiting)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            if (requestedIsFrontFacing) {
                int rearCameraFPS = requestedFPS;
                requestedFPS = 15;
                StartCoroutine (_InitializeCV ());
                requestedFPS = rearCameraFPS;
            } else {
                StartCoroutine (_InitializeCV ());
            }
#else
        StartCoroutine(_InitializeCV());
#endif
    }

    /// <summary>
    /// Initializes webcam texture by coroutine.
    /// </summary>
    private IEnumerator _InitializeCV()
    {
        if (hasInitDone)
            Dispose();

        isInitWaiting = true;

        // Creates the camera
        if (!String.IsNullOrEmpty(requestedDeviceName))
        {
            int requestedDeviceIndex = -1;
            if (Int32.TryParse(requestedDeviceName, out requestedDeviceIndex))
            {
                if (requestedDeviceIndex >= 0 && requestedDeviceIndex < WebCamTexture.devices.Length)
                {
                    webCamDevice = WebCamTexture.devices[requestedDeviceIndex];
                    webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                }
            }
            else
            {
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                {
                    if (WebCamTexture.devices[cameraIndex].name == requestedDeviceName)
                    {
                        webCamDevice = WebCamTexture.devices[cameraIndex];
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                        break;
                    }
                }
            }
            if (webCamTexture == null)
                Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
        }

        if (webCamTexture == null)
        {
            // Checks how many and which cameras are available on the device
            for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
            {
                if (WebCamTexture.devices[cameraIndex].isFrontFacing == requestedIsFrontFacing)
                {
                    webCamDevice = WebCamTexture.devices[cameraIndex];
                    webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                    break;
                }
            }
        }

        if (webCamTexture == null)
        {
            if (WebCamTexture.devices.Length > 0)
            {
                webCamDevice = WebCamTexture.devices[0];
                webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
            }
            else
            {
                Debug.LogError("Camera device does not exist.");
                isInitWaiting = false;
                yield break;
            }
        }

        // Starts the camera.
        webCamTexture.Play();

        while (true)
        {
            // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/).
#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
            if (webCamTexture.didUpdateThisFrame)
            {
#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2
                    while (webCamTexture.width <= 16) {
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    } 
#endif
#endif

                Debug.Log("name:" + webCamTexture.deviceName + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS);
                Debug.Log("videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                isInitWaiting = false;
                hasInitDone = true;

                OnCVInited();

                break;
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Releases all resource.
    /// </summary>
    private void Dispose()
    {
        isInitWaiting = false;
        hasInitDone = false;

        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
            webCamTexture = null;
        }
        if (rgbaMat != null)
        {
            rgbaMat.Dispose();
            rgbaMat = null;
        }
        if (texture != null)
        {
            Texture2D.Destroy(texture);
            texture = null;
        }
    }

    /// <summary>
    /// Raises the webcam texture initialized event.
    /// </summary>
    private void OnCVInited()
    {
        if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
            colors = new Color32[webCamTexture.width * webCamTexture.height];
        if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
            texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

        rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

        // 出力先の設定するならココ. 参照: OpenCVForUnityExample.
        outputScreenQuad.setupScreenQuadAndCamera(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

        Mat matOrg = Imgcodecs.imread(Utils.getFilePath("hand1.png"));
        mat = new Mat(matOrg.size(), CvType.CV_8UC3);
        Imgproc.cvtColor(matOrg, mat, Imgproc.COLOR_BGRA2RGB);
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        Dispose();
    }


    void Update()
    {
        if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
        {
            if (doneSetThreshlod) {
                Process();
            } else {
                ProcessSetting();
            }


        }

    }

    bool doneSetThreshlod = false;
    bool isSetClicked = false;

    public void OnSetClick() {

        isSetClicked = true;
    }

    public double cr_threshold_upper;
    public double cr_threshold_lower;
    public double s_threshold_upper;
    public double s_threshold_lower;
    public double v_threshold_upper;
    public double v_threshold_lower;


    void ProcessSetting() {
        Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
        Mat cameraMat = new Mat(rgbaMat.size(), CvType.CV_8UC3);
        Imgproc.cvtColor(rgbaMat, cameraMat, Imgproc.COLOR_RGBA2RGB);

        Mat dst = new Mat(cameraMat.size(), cameraMat.type());
        Mat _mat = new Mat(cameraMat.size(), cameraMat.type());
        Imgproc.resize(mat, _mat, _mat.size());
        Core.addWeighted(cameraMat, 0.5, _mat, 0.5, 0.0, dst);

        if (isSetClicked)
        {
            Mat handGray = new Mat(_mat.size(), CvType.CV_8UC1);
            Imgproc.cvtColor(_mat, handGray, Imgproc.COLOR_RGB2GRAY);
            Imgproc.threshold(handGray, handGray, 10.0, 255.0, Imgproc.THRESH_BINARY);
            Imgproc.erode(handGray, handGray, Imgproc.getStructuringElement(Imgproc.MORPH_ERODE, new Size(15, 15)), new Point(-1, -1), 8);

            outputScreenQuad.setMat(handGray);

            var hsvChs = ARUtil.getHSVChannels(cameraMat);
            var yCrCbChs = ARUtil.getYCrCbChannels(cameraMat);

            foreach (var chStr in new List<string> { "s", "v", "cr" })
            {
                MatOfDouble meanMat = new MatOfDouble();
                MatOfDouble stddevMat = new MatOfDouble();
                Mat chMat = new Mat();
                if (chStr == "s")
                {
                    chMat = hsvChs[1];
                }
                else if (chStr == "v")
                {
                    chMat = hsvChs[2];
                }
                else
                {
                    chMat = yCrCbChs[1];
                }
                Core.meanStdDev(chMat, meanMat, stddevMat, handGray);
                var mean = meanMat.toList()[0];
                var stddev = stddevMat.toList()[0];

                if (chStr == "s")
                {
                    s_threshold_lower = mean - stddev;
                    s_threshold_upper = mean + stddev;
                }
                else if (chStr == "v")
                {
                    v_threshold_lower = mean - stddev ;
                    v_threshold_upper = mean + stddev * 1.96;
                }
                else
                {
                    cr_threshold_lower = mean - stddev;
                    cr_threshold_upper = mean + stddev;
                }

            }
            doneSetThreshlod = true;

        } else {
            outputScreenQuad.setMat(dst);
        }


    }

    BinaryMatCreator binaryMatCreator;

    void Process()
    {
        Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
        Mat cameraMat = new Mat(rgbaMat.size(), CvType.CV_8UC3);
        Imgproc.cvtColor(rgbaMat, cameraMat, Imgproc.COLOR_RGBA2RGB);

        // BinaryCreator初期化
        binaryMatCreator = new BinaryMatCreator();
        binaryMatCreator.setCrUpper(cr_threshold_upper);
        binaryMatCreator.setCrLower(cr_threshold_lower);
        binaryMatCreator.setSUpper(s_threshold_upper);
        binaryMatCreator.setSLower(s_threshold_lower);
        binaryMatCreator.setVUpper(v_threshold_upper);
        binaryMatCreator.setVLower(v_threshold_lower);

        Mat mat = binaryMatCreator.createBinaryMat(cameraMat, new OpenCVForUnity.Rect(0, 0, cameraMat.width(), cameraMat.height()));

        outputScreenQuad.setMat(mat);
    }

}
