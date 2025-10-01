using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Wave;
using NAudio.Dsp;

namespace Audidesk
{
    public partial class RtsaWindow : Window
    {
        private WasapiLoopbackCapture? capture;
        private const int fftLength = 1024;
        private Complex[] fftBuffer = new Complex[fftLength];
        private int bufferOffset = 0;
        private int drawPoints = 980;

        private double[] previousDbValues;

        public RtsaWindow()
        {
            InitializeComponent();
            Loaded += RtsaWindow_Loaded;

            previousDbValues = new double[drawPoints];
            for (int i = 0; i < drawPoints; i++) previousDbValues[i] = -80; // 最低レベルで初期化
        }

        private void RtsaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartLoopbackAudio();
        }

        private void StartLoopbackAudio()
        {
            capture = new WasapiLoopbackCapture();
            capture.DataAvailable += OnDataAvailable;
            capture.StartRecording();
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            int bytesPerSample = 4; // 32-bit float

            for (int i = bufferOffset; i < fftLength; i++)
            {
                fftBuffer[i].X = 0;
                fftBuffer[i].Y = 0;
            }

            for (int i = 0; i < e.BytesRecorded; i += bytesPerSample)
            {
                if (bufferOffset >= fftLength) break;

                float sample32 = BitConverter.ToSingle(e.Buffer, i);
                fftBuffer[bufferOffset].X = (float)(sample32 * FastFourierTransform.HannWindow(bufferOffset, fftLength));
                fftBuffer[bufferOffset].Y = 0;
                bufferOffset++;

                if (bufferOffset >= fftLength)
                {
                    bufferOffset = 0;
                    FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);
                    Dispatcher.Invoke(DrawSpectrum);
                }
            }
        }

        private void DrawSpectrum()
        {
            SpectrumCanvas.Children.Clear();

            double width = SpectrumCanvas.ActualWidth;
            double height = SpectrumCanvas.ActualHeight;
            if (width == 0 || height == 0) return;

            // 背景グラデーション
            SpectrumCanvas.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(10, 10, 30), 0.0),
                    new GradientStop(Color.FromRgb(40, 40, 70), 1.0),
                }, new Point(0, 0), new Point(0, 1));

            var polyline = new Polyline
            {
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round,
                SnapsToDevicePixels = true,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Cyan,
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.7
                }
            };

            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.Magenta, 0.0),
                    new GradientStop(Colors.Cyan, 0.5),
                    new GradientStop(Colors.Lime, 1.0),
                }
            };

            polyline.Stroke = gradientBrush;

            double smoothing = 0.17;

            // 「よく動く」帯域をここで指定（ビン番号）
            int centerStart = 400; // 中央に表示したい帯域の開始ビン（例）
            int centerEnd = 600;   // 中央に表示したい帯域の終了ビン（例）
            int centerRange = centerEnd - centerStart;

            for (int i = 0; i < drawPoints; i++)
            {
                double normPos = (double)i / (drawPoints - 1);
                int bin;

                if (normPos < 0.45)
                {
                    // 左端: 低周波数帯域(0〜centerStartの45%)
                    double leftNorm = normPos / 0.45; // 0~1
                    bin = (int)(leftNorm * centerStart);
                }
                else if (normPos < 0.55)
                {
                    // 中央: よく動く帯域(centerStart〜centerEnd)
                    double centerNorm = (normPos - 0.45) / 0.10; // 0~1
                    bin = centerStart + (int)(centerRange * centerNorm);
                }
                else
                {
                    // 右端: 高周波数帯域(centerEnd〜fftLength/2)
                    double rightNorm = (normPos - 0.55) / 0.45; // 0~1
                    bin = centerEnd + (int)((fftLength / 2 - centerEnd) * rightNorm);
                }

                bin = Math.Clamp(bin, 0, fftLength / 2 - 1);

                double magnitude = Math.Sqrt(fftBuffer[bin].X * fftBuffer[bin].X + fftBuffer[bin].Y * fftBuffer[bin].Y);
                magnitude = Math.Max(magnitude, 1e-10);

                double db = 20 * Math.Log10(magnitude);
                db = Math.Clamp(db, -80, 0);

                db = previousDbValues[i] = previousDbValues[i] * (1 - smoothing) + db * smoothing;

                double x = normPos * width;
                double y = height - ((db + 80) / 80.0) * height;

                polyline.Points.Add(new Point(x, y));
            }

            SpectrumCanvas.Children.Add(polyline);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            capture?.StopRecording();
            capture?.Dispose();
        }
    }
}
