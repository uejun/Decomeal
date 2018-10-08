using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using Vuforia;

public class ARMainDrink : MonoBehaviour {

    // テクスチャ変換のオンオフのためのフラグ
    public static bool willChange = true;
    // 飲料領域を矩形を描画(デバッグ用)
    public bool shouldDrawRect;

    public bool startProcess;

    bool doneSetThreshlod;

    // 入力用
    Texture2D cameraTexture; // UnityEngineから取得する画像
    Mat cameraMat; // OpenCVの入力画像. cameraTextureから毎フレームはじめに変換して得る

    // 食品領域に貼りつけるテクスチャ. 
    Mat texture;

    // 出力用
    public GameObject outputCamera1; // 変換後の画像を撮影するためのカメラ. インスペクタで設定してください
    OutputCamQuad outputScreenQuad; // outputCamera1が撮影するQuad. 変換後の画像を投影する

    public double alpha = 0.1;

    BinaryMatCreator binaryMatCreator;
    ColorTextureCreator _textureCreator;

    // 前回飲料領域を保持
    Region foodRegion;

    // コップターゲット (Cylinder Target)
    Cup cup;

    public string TargetObjectName = "CupCylinderTarget";

    public double cr_threshold_upper;
    public double cr_threshold_lower;
    public double s_threshold_upper = 230;
    public double s_threshold_lower = 50;
    public double v_threshold_upper;
    public double v_threshold_lower;

    public GameObject outputCamera2;
    OutputCamQuad camQuad2;

    public GameObject outputCamera3;
    OutputCamQuad camQuad3;

    public GameObject outputCamera4;
    OutputCamQuad camQuad4;


    Image.PIXEL_FORMAT mPixelFormat = Image.PIXEL_FORMAT.UNKNOWN_FORMAT;
    bool mAccessCameraImage = true;
    bool mFormatRegistered = false;
    Vuforia.Image image;

    int H_sourceMean;

    void Start()
    {
        #if UNITY_EDITOR
        mPixelFormat = Image.PIXEL_FORMAT.RGBA8888; // Need Grayscale for Editor
        #else
        mPixelFormat = Image.PIXEL_FORMAT.RGB888; // Use RGB888 for mobile
        #endif

        //VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        //VuforiaARController.Instance.RegisterTrackablesUpdatedCallback(OnTrackablesUpdated);

        // 入力画像用 初期
        cameraMat = new Mat(Screen.height, Screen.width, CvType.CV_8UC3);
        cameraTexture = new Texture2D(cameraMat.cols(), cameraMat.rows(), TextureFormat.RGBA32, false);

        // 出力画面 初期化
        outputScreenQuad = outputCamera1.GetComponent<OutputCamQuad>();

        camQuad2 = outputCamera2.GetComponent<OutputCamQuad>();
        camQuad3 = outputCamera3.GetComponent<OutputCamQuad>();
        camQuad4 = outputCamera4.GetComponent<OutputCamQuad>();

        // コップの初期化
        var cupTargetBehaviour = GameObject.Find(TargetObjectName).GetComponent<CylinderTargetBehaviour>();
        cup = new Cup(cupTargetBehaviour, cameraMat.rows());

        // 飲料領域の初期化
        foodRegion = new Region(0, 0, cameraMat.cols(), cameraMat.rows());

        // テクスチャCreator初期化
        _textureCreator = new ColorTextureCreator(30, 100, 100, 1.0);

        // BinaryCreator初期化
        binaryMatCreator = new BinaryMatCreator();
        binaryMatCreator.setCrUpper(cr_threshold_upper);
        binaryMatCreator.setCrLower(cr_threshold_lower);
        binaryMatCreator.setSUpper(s_threshold_upper);
        binaryMatCreator.setSLower(s_threshold_lower);
        binaryMatCreator.setVUpper(v_threshold_upper);
        binaryMatCreator.setVLower(v_threshold_lower);
    }

    void OnVuforiaStarted()
    {
        // Vuforia has started, now register camera image format

        // Try register camera image format
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true))
        {
            //Debug.Log("Successfully registered pixel format " + mPixelFormat.ToString());

            mFormatRegistered = true;

        }
        else
        {
            //Debug.LogError(
                //"\nFailed to register pixel format: " + mPixelFormat.ToString() +
                //"\nThe format may be unsupported by your device." +
                //"\nConsider using a different pixel format.\n");

            mFormatRegistered = false;
        }
    }

    void OnPostRender()
    {
        if (doneSetThreshlod)
        {
            Process();
        }
        else
        {
            ProcessCalibration();
        }
    }


    /// <summary>
    /// Called each time the Vuforia state is updated
    /// </summary>
    void OnTrackablesUpdated()
    {
        if (mFormatRegistered)
        {
            if (mAccessCameraImage)
            {
                image = CameraDevice.Instance.GetCameraImage(mPixelFormat);

                if (image != null)
                {
                    //Debug.Log(
                    //    "\nImage Format: " + image.PixelFormat +
                    //    "\nImage Size:   " + image.Width + "x" + image.Height +
                    //    "\nBuffer Size:  " + image.BufferWidth + "x" + image.BufferHeight +
                    //    "\nImage Stride: " + image.Stride + "\n"
                    //);

                    byte[] pixels = image.Pixels;

                    if (pixels != null && pixels.Length > 0)
                    {
                        //Debug.Log(
                        //    "\nImage pixels: " +
                        //    pixels[0] + ", " +
                        //    pixels[1] + ", " +
                        //    pixels[2] + ", ...\n"
                        //);
                    }
                
                }
            }
        }
    }

    void ProcessCalibration()
    {
        // UnityのTexture2DからOpencvのMatに変換
        int imageWidth = cameraTexture.width;
        int imageHeight = cameraTexture.height;
        UnityEngine.Rect wholeRect = new UnityEngine.Rect(0, 0, cameraTexture.width, cameraTexture.height);
        cameraTexture.ReadPixels(wholeRect, 0, 0, true);
        //cameraMat = new Mat(imageHeight, imageWidth, CvType.CV_8UC3);
        //cameraTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
        //image.CopyToTexture(cameraTexture);
        Utils.texture2DToMat(cameraTexture, cameraMat);


        Mat gray = new Mat(imageHeight, imageWidth, CvType.CV_8UC1);
        Imgproc.cvtColor(cameraMat, gray, Imgproc.COLOR_RGB2GRAY);

        Mat grayC3 = new Mat(imageHeight, imageWidth, CvType.CV_8UC3);
        Imgproc.cvtColor(gray, grayC3, Imgproc.COLOR_GRAY2RGB);

        int rectW = (int) (imageHeight * 0.4);
        int rectH = (int)(imageHeight * 0.3);
        var x =(int)(imageWidth * 0.5 - (rectW / 2));
        var y = (int)(imageHeight * 0.5 - (rectH / 2));
        var rect = new OpenCVForUnity.Rect(x, y, rectW, rectH);

        var center = new Point(imageWidth / 2.0, imageHeight / 2.0);
        var lineColor = new Scalar(255, 153, 153);

        var rotatedRect = new RotatedRect(center, new Size(rectW, rectH), 0);
        var rotatedSmallRect = new RotatedRect(center, new Size((int)(rectW * 0.7), (int)(rectH * 0.7)), 0);
        
        Imgproc.ellipse(grayC3, rotatedRect, lineColor, 3);
        Imgproc.ellipse(grayC3, rotatedSmallRect, lineColor, 3);

        //outputScreenQuad.setMat(grayC3);

        if (startProcess) {
            var mask = Mat.zeros(imageHeight, imageWidth, CvType.CV_8UC1);
            Imgproc.ellipse(mask, rotatedRect, new Scalar(255), -1);

            var hsvChs = ARUtil.getHSVChannels(cameraMat);
            var yCrCbChs = ARUtil.getYCrCbChannels(cameraMat);

            foreach(var chStr in new List<string>{"s", "v", "cr"}) {
                MatOfDouble meanMat = new MatOfDouble();
                MatOfDouble stddevMat = new MatOfDouble();
                Mat chMat = new Mat();
                if (chStr == "s") {
                    chMat = hsvChs[1];
                } else if (chStr == "v") {
                    chMat = hsvChs[2];
                } else {
                    chMat = yCrCbChs[1];
                }
                Core.meanStdDev(chMat, meanMat, stddevMat, mask);
                var mean = meanMat.toList()[0];
                var stddev = stddevMat.toList()[0];

                // 95%信頼区間
                if (chStr == "s") {
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

            H_sourceMean = (int)(Core.mean(hsvChs[0], mask).val[0]);

            doneSetThreshlod = true;

        }
        else {
            outputScreenQuad.setMat(grayC3);
        }

    }


    // Update is called once per frame
    void Process()
    {
        binaryMatCreator.setCrUpper(cr_threshold_upper);
        binaryMatCreator.setCrLower(cr_threshold_lower);
        binaryMatCreator.setSUpper(s_threshold_upper);
        binaryMatCreator.setSLower(s_threshold_lower);
        binaryMatCreator.setVUpper(v_threshold_upper);
        binaryMatCreator.setVLower(v_threshold_lower);

        // UnityのTexture2DからOpencvのMatに変換
        int imageWidth = cameraTexture.width;
        int imageHeight = cameraTexture.height;
        UnityEngine.Rect wholeRect = new UnityEngine.Rect(0, 0, cameraTexture.width, cameraTexture.height);
        cameraTexture.ReadPixels(wholeRect, 0, 0, true);
        //int imageWidth = image.Width;
        //int imageHeight = image.Height;
        //cameraMat = new Mat(imageHeight, imageWidth, CvType.CV_8UC3);
        //cameraTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
        //image.CopyToTexture(cameraTexture);
        Utils.texture2DToMat(cameraTexture, cameraMat);


        //var hsvChs = ARUtil.getHSVChannels(cameraMat);
        //var yCrCbChs = ARUtil.getYCrCbChannels(cameraMat);
        //var sBy = new Mat(cameraMat.size(), CvType.CV_8UC1);
        //var crBy = new Mat(cameraMat.size(), CvType.CV_8UC1);
        //var vBy = new Mat(cameraMat.size(), CvType.CV_8UC1);
        //Core.inRange(hsvChs[1], new Scalar(s_threshold_lower), new Scalar(s_threshold_upper), sBy);
        //Core.inRange(yCrCbChs[1], new Scalar(cr_threshold_lower), new Scalar(cr_threshold_upper), crBy);
        //Core.inRange(hsvChs[2], new Scalar(v_threshold_lower), new Scalar(v_threshold_upper), vBy);
        //camQuad2.setMat(sBy);
        //camQuad3.setMat(crBy);
        //camQuad4.setMat(vBy);
        //goto show;

        /* 初期化 */
        IScorer scorer = null; // 領域判定のスコアラー
        OpenCVForUnity.Rect searchRect = null; // 探索領域矩形
 
        // Vuforiaでのコップ検出可否によって、以下を切り替える
        // ・探索対象領域
        // ・スコアー戦略
        // ・二値化の閾値
        if (cup.isNotFound())
        {
            // TODO: 高速化のため、対象物の探索領域を前回領域に限定する
            //if (coffeeRegion.circularity > 0.50) {
            //  searchRect = coffeeRegion.predictNextSearchRect ();
            //  searchRect = ARUtil.calcRectWithinMat (searchRect, cameraMat);
            //} else {
            //  searchRect = new OpenCVForUnity.Rect (0, 0, cameraMat.cols (), cameraMat.rows ());
            //}
            searchRect = new OpenCVForUnity.Rect(0, 0, cameraMat.cols(), cameraMat.rows());
        }
        else if (cup.isTracked())
        {
            print("Cup is tracked.");
            cup.update();
            // カップの上面の矩形を探索対象矩形とする. 
            searchRect = cup.getTopSurfaceRect(cameraMat);
        }

        scorer = new OrangeScorer(searchRect, foodRegion.rect);

        try
        {
            // カメラ入力画像から, searchRectサイズの二値画像を生成
            Mat binaryROIMat = binaryMatCreator.createBinaryMat(cameraMat, searchRect);

            // 二値画像&探索領域矩形で輪郭探索
            var contours = ARUtil.findContours(binaryROIMat, searchRect.tl());

            // 飲料領域候補群を作成 -> 円形度で除外 -> 候補にスコアをつける -> 最大スコアの候補を取得
            var regionSet = new RegionCandidateSet(contours)
                .elliminateByArea(searchRect, 0.01, 0.9)
                .elliminateByCircularity(0.1)
                .score(scorer)
                .sort();

            var count = 0;
            var regions = new List<Region>();
            foreach (var candidate in regionSet.candidates)
            {
                if (count > 5)
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
            }

            // Regionのマスクを合体
            Mat alphaMask = Mat.zeros(cameraMat.size(), CvType.CV_8U);
            foreach (var region in regions)
            {
                Core.add(region.mask, alphaMask, alphaMask);
            }

            // テクスチャ作成
            texture = _textureCreator.create(cameraMat, alphaMask, H_sourceMean, 0, 0);

            if (willChange)
            {
                // アルファブレンド
                ARUtil.alphaBlend(cameraMat, texture, alpha, alphaMask);
            }

            //Debug.Log (candidate.circularity);
            // 矩形描画
            if (shouldDrawRect)
            {
                Imgproc.rectangle (cameraMat, foodRegion.rect.tl (), foodRegion.rect.br (), new Scalar (0, 255, 0), 3);
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

    public void OnStartProcessClick() {
        startProcess = true;
    }

    public void SetTargetColor(ColorTextureCreator creator) {
        _textureCreator = creator;
    }
}
