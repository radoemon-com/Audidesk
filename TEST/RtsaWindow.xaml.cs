using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Wave;
using NAudio.Dsp;
using System.Windows.Controls;

namespace Audidesk
{
    public partial class RtsaWindow : Window
    {
        private WasapiLoopbackCapture? capture;
        private const int fftLength = 1024;
        private Complex[] fftBuffer = new Complex[fftLength];
        private int bufferOffset = 0;
        private int barsCount = 80;

        private double[] previousDbValues;
        private const int sampleRate = 44100;
        private const double freqStart = 80.0;
        private const double freqEnd = 6000.0;

        public RtsaWindow()
        {
            InitializeComponent();
            Loaded += RtsaWindow_Loaded;

            previousDbValues = new double[barsCount];
            for (int i = 0; i < barsCount; i++) previousDbValues[i] = -60;
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
            int bytesPerSample = 4;

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

            double centerY = height / 2;

            SpectrumCanvas.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(10, 10, 30), 0.0),
                    new GradientStop(Color.FromRgb(40, 40, 70), 1.0),
                }, new Point(0, 0), new Point(0, 1));

            double barWidth = width / barsCount * 0.8;
            double barSpacing = width / barsCount * 0.2;

            double smoothing = 0.05;

            for (int i = 0; i < barsCount; i++)
            {
                double freq = freqStart + (freqEnd - freqStart) * i / (barsCount - 1);
                int bin = (int)(freq / sampleRate * fftLength);
                bin = Math.Clamp(bin, 0, fftLength / 2 - 1);

                double magnitude = Math.Sqrt(fftBuffer[bin].X * fftBuffer[bin].X + fftBuffer[bin].Y * fftBuffer[bin].Y);
                magnitude = Math.Max(magnitude, 1e-10);

                double db = 20 * Math.Log10(magnitude);
                db = Math.Clamp(db, -60, 0);

                // スムージング
                previousDbValues[i] = previousDbValues[i] * (1 - smoothing) + db * smoothing;

                // 正規化（0.0 ～ 1.0）
                double normalized = (previousDbValues[i] + 60) / 60.0;

                // 棒の高さ（上下対称）
                double barHeight = normalized * (height / 2) * 1.2;
                barHeight = Math.Min(barHeight, height / 2);

                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight * 2, // 上下に伸ばすため2倍
                    Fill = new LinearGradientBrush(
                        Colors.Cyan,
                        Colors.Magenta,
                        new Point(0, 1),
                        new Point(0, 0)),
                    RadiusX = barWidth / 4,
                    RadiusY = barWidth / 4,
                };

                // 棒の配置（中央から上下に出る）
                Canvas.SetLeft(rect, i * (barWidth + barSpacing));
                Canvas.SetTop(rect, centerY - barHeight);

                SpectrumCanvas.Children.Add(rect);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            capture?.StopRecording();
            capture?.Dispose();
        }
    }
}
