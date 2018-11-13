using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using OpenCVForUnityExample;

public class ARMainSushi : MonoBehaviour {

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
    Mat rgbMat;

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


    // テクスチャ変換のオンオフのためのフラグ
    public static bool willChange = true;
    // 飲料領域を矩形を描画(デバッグ用)
    public bool shouldDrawRect;

    public bool startProcess;

    bool doneSetThreshlod;

    // 入力用
    Mat cameraMat; // OpenCVの入力画像. cameraTextureから毎フレームはじめに変換して得る

    // 食品領域に貼りつけるテクスチャ. 
    Mat decorateTextureMat;

    // 出力用
    public GameObject outputCamera1; // 変換後の画像を撮影するためのカメラ. インスペクタで設定してください
    OutputCamQuad outputScreenQuad; // outputCamera1が撮影するQuad. 変換後の画像を投影する

    public double alpha = 0.1;

    BinaryMatCreator binaryMatCreator;

    // 前回飲料領域を保持
    Region foodRegion;

    public double cr_threshold_upper;
    public double cr_threshold_lower;
    public double s_threshold_upper;
    public double s_threshold_lower;
    public double v_threshold_upper;
    public double v_threshold_lower;

    public GameObject outputCamera2;
    OutputCamQuad camQuad2;

    public GameObject outputCamera3;
    OutputCamQuad camQuad3;

    public GameObject outputCamera4;
    OutputCamQuad camQuad4;

    public bool isDebug;

    SushiTextureCreator _textureCreator;

    Mat floodFillMask;
    IScorer scorer; // 領域判定のスコアラー

    void Start()
    {
        InitializeCV();

        // 出力画面 初期化
        outputScreenQuad = outputCamera1.GetComponent<OutputCamQuad>();

        camQuad2 = outputCamera2.GetComponent<OutputCamQuad>();
        camQuad3 = outputCamera3.GetComponent<OutputCamQuad>();
        camQuad4 = outputCamera4.GetComponent<OutputCamQuad>();
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
        rgbMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

        // 出力先の設定するならココ. 参照: OpenCVForUnityExample.
        outputScreenQuad.setupScreenQuadAndCamera(Screen.height, Screen.width, CvType.CV_8UC3);

        // 飲料領域の初期化
        foodRegion = new Region(0, 0, rgbaMat.cols(), rgbaMat.rows());

        // テクスチャマネージャーの初期化
        _textureCreator = new SushiTextureCreator();
    
        // BinaryCreator初期化
        binaryMatCreator = new BinaryMatCreator();
        binaryMatCreator.setCrUpper(cr_threshold_upper);
        binaryMatCreator.setCrLower(cr_threshold_lower);
        binaryMatCreator.setSUpper(s_threshold_upper);
        binaryMatCreator.setSLower(s_threshold_lower);
        binaryMatCreator.setVUpper(v_threshold_upper);
        binaryMatCreator.setVLower(v_threshold_lower);
    }


    void Update()
    {
        if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
        {
            if (!doneSetThreshlod)
            {
                ProcessCalibration();
            }
            else
            {
                Process();
            }
        }

    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        Dispose();
    }


    void ProcessCalibration()
    {

        Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        int imageWidth = Screen.width;
        int imageHeight = Screen.height;
        Mat cameraMat = new Mat(new Size(imageWidth, imageHeight), CvType.CV_8UC3);
        Imgproc.resize(rgbMat, cameraMat, cameraMat.size());

        //Mat cameraMat = new Mat(rgbaMat.size(), CvType.CV_8UC3);
        //Imgproc.cvtColor(rgbaMat, cameraMat, Imgproc.COLOR_RGBA2RGB);
      
        Mat gray = new Mat(imageHeight, imageWidth, CvType.CV_8UC1);
        Imgproc.cvtColor(cameraMat, gray, Imgproc.COLOR_RGB2GRAY);

        Mat grayC3 = new Mat(imageHeight, imageWidth, CvType.CV_8UC3);
        Imgproc.cvtColor(gray, grayC3, Imgproc.COLOR_GRAY2RGB);

        int rectW = (int)(imageHeight * 0.4);
        int rectH = (int)(imageHeight * 0.2);
        var x = (int)(imageWidth * 0.5 - (rectW / 2));
        var y = (int)(imageHeight * 0.5 - (rectH / 2));
        var rect = new OpenCVForUnity.Rect(x, y, rectW, rectH);

        var center = new Point(imageWidth / 2.0, imageHeight / 2.0);
        var lineColor = new Scalar(255, 153, 153);
        var thickness = 3;

        Imgproc.rectangle(grayC3, rect, lineColor, thickness);

        //outputScreenQuad.setMat(grayC3);

        if (startProcess)
        {
            var mask = Mat.zeros(imageHeight, imageWidth, CvType.CV_8UC1);
            Imgproc.rectangle(mask, rect, new Scalar(255), -1);

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
                Core.meanStdDev(chMat, meanMat, stddevMat, mask);
                var mean = meanMat.toList()[0];
                var stddev = stddevMat.toList()[0];

                // 95%信頼区間
                if (chStr == "s")
                {
                    s_threshold_lower = mean - stddev * 1.96;
                    s_threshold_upper = mean + stddev * 1.96;
                }
                else if (chStr == "v")
                {
                    v_threshold_lower = mean - stddev * 1.96;
                    v_threshold_upper = mean + stddev * 1.96;
                }
                else
                {
                    cr_threshold_lower = mean - stddev * 1.96;
                    cr_threshold_upper = mean + stddev * 1.96;
                }

            }

            doneSetThreshlod = true;

        }
        else
        {
            outputScreenQuad.setMat(grayC3);
        }

    }


    void Process()
    {
        binaryMatCreator = new BinaryMatCreator();
        binaryMatCreator.setCrUpper(cr_threshold_upper + 30);
        binaryMatCreator.setCrLower(cr_threshold_lower - 30);
        binaryMatCreator.setSUpper(s_threshold_upper + 30);
        binaryMatCreator.setSLower(s_threshold_lower - 30);
        binaryMatCreator.setVUpper(v_threshold_upper + 80);
        binaryMatCreator.setVLower(v_threshold_lower - 80);

        Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);

        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        int imageWidth = Screen.width;
        int imageHeight = Screen.height;
        Mat cameraMat = new Mat(new Size(imageWidth, imageHeight), CvType.CV_8UC3);
        Imgproc.resize(rgbMat, cameraMat, cameraMat.size());

        //Mat cameraMat = new Mat(rgbaMat.size(), CvType.CV_8UC3);
        //Imgproc.cvtColor(rgbaMat, cameraMat, Imgproc.COLOR_RGBA2RGB);

        var hSVChannels = ARUtil.getHSVChannels(cameraMat);
        var yCrCbChannels = ARUtil.getYCrCbChannels(cameraMat);

        OpenCVForUnity.Rect searchRect = new OpenCVForUnity.Rect(0, 0, cameraMat.cols(), cameraMat.rows());


        // 領域判定のスコア
        scorer = new MaguroScorer(searchRect);


        try
        {
        
            // カメラ入力画像から, searchRectサイズの二値画像を生成
            Mat binaryROIMat = binaryMatCreator.createBinaryMat(cameraMat, searchRect);

            // 二値画像&探索領域矩形で輪郭探索
            var contours = ARUtil.findContours(binaryROIMat, searchRect.tl());

            // 領域候補群を作成 -> 候補にスコアをつける -> 最大スコアの候補を取得
            var regionSet = new RegionCandidateSet(contours)
                .elliminateByArea(searchRect, 0.01, 0.9)
                .score(scorer)
                .sort();

            if (regionSet.candidates.Count == 0)
            {
                print("first candidates is 0");
                goto show;
            }

            var count = 0;
            var regions = new List<Region>();
            foreach (var candidate in regionSet.candidates)
            {
                if (count > 2)
                {
                    break;
                }
                if (candidate == null)
                {
                    print("candite is null");
                    break;

                }
                // 領域作成
                foodRegion = new Region(candidate, cameraMat);
                regions.Add(foodRegion);
                count++;
            }


            //var regions = new List<Region>();
            //var cr_refined_threshold = 125;

            //foreach (var candidate in regionSet.candidates)
            //{

            //    // 領域作成
            //    var region = new Region(candidate, cameraMat);
            //    var refinedRegion = region.createRefienedRegion(yCrCbChannels[1], cr_refined_threshold);
            //    if (refinedRegion != null)
            //    {
            //        regions.Add(refinedRegion);
            //    }
            //}

            //var filteredRegions = Region.elliminateByInclusionRect(regions);


            // 食品領域に貼りつけるテクスチャ作成
            var texture = _textureCreator.create(cameraMat, regions);

            if (texture == null)
            {
                print("regions is empty");
                goto show;
            }

            Mat alphaMask = Mat.zeros(cameraMat.size(), CvType.CV_8U);
            foreach (var region in regions)
            {
                Core.add(region.mask, alphaMask, alphaMask);
            }

            if (willChange)
            {
                _textureCreator.alphaBlend(cameraMat, texture, alphaMask, alpha);
            }

            if (shouldDrawRect) {
                // foodRegion.drawRect(matForDeveloper);
                Imgproc.rectangle(cameraMat, searchRect.tl(), searchRect.br(), new Scalar(0, 0, 255), 3);
            }

        }
        catch (System.Exception e)
        {
            print(e);
            goto show;
        }

    show:
        outputScreenQuad.setMat(cameraMat);

    }

    public void OnStartProcessClick()
    {
        startProcess = true;
    }


    //void showChannelBinary(List<Mat> hsvChannels, List<Mat> yCrCbChannels)
    //{
    //    Mat binaryMat_V = new Mat(cameraMat.size(), CvType.CV_8UC1);
    //    Imgproc.threshold(hsvChannels[2], binaryMat_V, v_low_threshold, 255, Imgproc.THRESH_BINARY);

    //    Mat binaryMat_Cr = new Mat(cameraMat.size(), CvType.CV_8UC1);
    //    Imgproc.threshold(yCrCbChannels[1], binaryMat_Cr, cr_low_threshold, 255, Imgproc.THRESH_BINARY);
    //    //Core.bitwise_and(binaryROIMat_H_low, binaryROIMat_H_up, binaryROIMat_H);

    //    Mat binaryMat_Cb = new Mat(cameraMat.size(), CvType.CV_8UC1);
    //    Imgproc.threshold(yCrCbChannels[2], binaryMat_Cb, cb_low_threshold, 255, Imgproc.THRESH_BINARY);


    //    Mat binaryMat_S = new Mat(cameraMat.size(), CvType.CV_8UC1);
    //    Imgproc.threshold(hsvChannels[1], binaryMat_S, s_threshold, 255, Imgproc.THRESH_BINARY);

    //    var wiseAndBig = _targetTracker.binalize(cameraMat, new OpenCVForUnity.Rect(0, 0, cameraMat.cols(), cameraMat.rows()));

    //    camQuad3.setMat(binaryMat_Cr);
    //    camQuad4.setMat(binaryMat_S);
    //}

}
