using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class ARNoodle : ARMainCV {

    // テクスチャ変換のオンオフのためのフラグ
    public static bool willChange = true;
    // 飲料領域を矩形を描画(デバッグ用)
    public bool shouldDrawRect;

    // 出力用
    public GameObject outputCamera1; // 変換後の画像を撮影するためのカメラ. インスペクタで設定してください
    OutputCamQuad outputScreenQuad; // outputCamera1が撮影するQuad. 変換後の画像を投影する

    // 前回飲料領域を保持
    Region foodRegion;
    BinaryMatCreator binaryMatCreator;


    public double alpha = 0.1;

    public double cr_threshold_upper;
    public double cr_threshold_lower;
    public double s_threshold_upper = 230;
    public double s_threshold_lower = 50;
    public double v_threshold_upper;
    public double v_threshold_lower;

    bool doneSetThreshlod = false;
    bool startProcess = false;

    NoodleTextureCreator _textureCreator;

    public override void OnARMainInited()
    {
        // 出力画面 初期化
        outputScreenQuad = outputCamera1.GetComponent<OutputCamQuad>();

        //camQuad2 = outputCamera2.GetComponent<OutputCamQuad>();
        //camQuad3 = outputCamera3.GetComponent<OutputCamQuad>();
        //camQuad4 = outputCamera4.GetComponent<OutputCamQuad>();

        // 出力先の設定するならココ. 参照: OpenCVForUnityExample.
        outputScreenQuad.setupScreenQuadAndCamera(Screen.height, Screen.width, CvType.CV_8UC3);

        // 飲料領域の初期化
        foodRegion = new Region(0, 0, rgbaMat.cols(), rgbaMat.rows());

        // テクスチャCreator初期化
        _textureCreator = new NoodleTextureCreator();
        _textureCreator.SetMatSize(webCamTexture.width, webCamTexture.height);


        // BinaryCreator初期化
        binaryMatCreator = new BinaryMatCreator();
        binaryMatCreator.setCrUpper(cr_threshold_upper);
        binaryMatCreator.setCrLower(cr_threshold_lower);
        binaryMatCreator.setSUpper(s_threshold_upper);
        binaryMatCreator.setSLower(s_threshold_lower);
        binaryMatCreator.setVUpper(v_threshold_upper);
        binaryMatCreator.setVLower(v_threshold_lower);
    }

    public override void Process()
    {
        if (!doneSetThreshlod)
        {
            _ProcessCalibration();
        }
        else
        {
            _Process();
        }
    }

    void _ProcessCalibration() {
        Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        int imageWidth = Screen.width;
        int imageHeight = Screen.height;
        Mat cameraMat = new Mat(new Size(imageWidth, imageHeight), CvType.CV_8UC3);
        Imgproc.resize(rgbMat, cameraMat, cameraMat.size());

        //Mat cameraMat = new Mat(rgbaMat.size(), rgbaMat.type());
        //rgbaMat.copyTo(cameraMat);

        Mat gray = new Mat(imageHeight, imageWidth, CvType.CV_8UC1);
        Imgproc.cvtColor(cameraMat, gray, Imgproc.COLOR_RGB2GRAY);

        Mat grayC3 = new Mat(imageHeight, imageWidth, CvType.CV_8UC3);
        Imgproc.cvtColor(gray, grayC3, Imgproc.COLOR_GRAY2RGB);

        int rectW = (int)(imageHeight * 0.4);
        int rectH = (int)(imageHeight * 0.3);
        var x = (int)(imageWidth * 0.5 - (rectW / 2));
        var y = (int)(imageHeight * 0.5 - (rectH / 2));
        var rect = new OpenCVForUnity.Rect(x, y, rectW, rectH);

        var center = new Point(imageWidth / 2.0, imageHeight / 2.0);
        var lineColor = new Scalar(255, 153, 153);

        var rotatedRect = new RotatedRect(center, new Size(rectW, rectH), 0);
        var rotatedSmallRect = new RotatedRect(center, new Size((int)(rectW * 0.7), (int)(rectH * 0.7)), 0);

        Imgproc.ellipse(grayC3, rotatedRect, lineColor, 3);
        Imgproc.ellipse(grayC3, rotatedSmallRect, lineColor, 3);

        //outputScreenQuad.setMat(grayC3);


        if (startProcess)
        {
            Debug.Log("startProcess");
            var mask = Mat.zeros(imageHeight, imageWidth, CvType.CV_8UC1);
            Imgproc.ellipse(mask, rotatedRect, new Scalar(255), -1);

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

                if (chStr == "s")
                {
                    s_threshold_lower = mean - stddev * 2 - 30;
                    s_threshold_upper = mean + stddev * 2 + 30;
                }
                else if (chStr == "v")
                {
                    v_threshold_lower = mean - stddev * 2 - 80;
                    v_threshold_upper = mean + stddev * 2 + 80;
                }
                else
                {
                    cr_threshold_lower = mean - stddev * 2 - 30;
                    cr_threshold_upper = mean + stddev * 2 + 30;
                }

            }


            doneSetThreshlod = true;

        }
        else
        {
            outputScreenQuad.setMat(grayC3);
        }

    }

    private int loopCount = 0;

    void _Process()
    {
        binaryMatCreator = new BinaryMatCreator();
        binaryMatCreator.setCrUpper(cr_threshold_upper);
        binaryMatCreator.setCrLower(cr_threshold_lower);
        binaryMatCreator.setSUpper(s_threshold_upper);
        binaryMatCreator.setSLower(s_threshold_lower);
        binaryMatCreator.setVUpper(v_threshold_upper);
        binaryMatCreator.setVLower(v_threshold_lower);

        Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

        int imageWidth = Screen.width;
        int imageHeight = Screen.height;
        Mat cameraMat = new Mat(new Size(imageWidth, imageHeight), CvType.CV_8UC3);
        Imgproc.resize(rgbMat, cameraMat, cameraMat.size());

        //Mat cameraMat = new Mat(rgbaMat.size(), CvType.CV_8UC3);
        //Imgproc.cvtColor(rgbaMat, cameraMat, Imgproc.COLOR_RGBA2RGB);

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
        // 探索領域矩形
        OpenCVForUnity.Rect searchRect = new OpenCVForUnity.Rect(0, 0, cameraMat.cols(), cameraMat.rows());
        var circularity = 0.2;

        // 領域判定のスコアラー
        IScorer scorer = null;
        if (loopCount == 0)
        {
            scorer = new OrangeScorer(searchRect, searchRect);
        }
        else
        {
            scorer = new OrangeScorer(searchRect, foodRegion.rect);
        }


        try
        {
            // カメラ入力画像から, searchRectサイズの二値画像を生成
            Mat binaryROIMat = binaryMatCreator.createBinaryMat(cameraMat, searchRect);

            // 二値画像&探索領域矩形で輪郭探索
            var contours = ARUtil.findContours(binaryROIMat, searchRect.tl());

            // 飲料領域候補群を作成 -> 円形度で除外 -> 候補にスコアをつける -> 最大スコアの候補を取得
            var regionSet = new RegionCandidateSet(contours)
                .elliminateByArea(searchRect, 0.01, 0.9)
                .score(scorer)
                .sort();

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


            // Regionのマスクを合体
            Mat alphaMask = Mat.zeros(cameraMat.size(), CvType.CV_8U);
            foreach (var region in regions)
            {
                Core.add(region.mask, alphaMask, alphaMask);
            }

            // テクスチャ作成
            var decorateTextureMat = _textureCreator.create(cameraMat, alphaMask);

            if (willChange)
            {
                // アルファブレンド
                ARUtil.alphaBlend(cameraMat, decorateTextureMat, alpha, alphaMask);
            }

            //Debug.Log (candidate.circularity);
            // 矩形描画
            if (shouldDrawRect)
            {
                Imgproc.rectangle(cameraMat, foodRegion.rect.tl(), foodRegion.rect.br(), new Scalar(0, 255, 0), 3);
            }

        }
        catch (System.Exception e)
        {
            print(e);
            goto show;
        }

    show:
        outputScreenQuad.setMat(cameraMat);
        loopCount++;

    }

    public void OnStartProcessClick()
    {
        startProcess = true;
    }

}
