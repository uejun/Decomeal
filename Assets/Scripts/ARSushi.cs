using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class ARSushi : ARMainCV {

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

    SushiTextureCreator _textureCreator;

    Mat floodFillMask;
    IScorer scorer; // 領域判定のスコアラー

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

        // テクスチャCreator
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

    void _ProcessCalibration()
    {
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

            if (shouldDrawRect)
            {
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
}
