using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using NAudio.Wave;

namespace Audidesk
{
    public partial class EffectWindow : Window
    {
        private IWavePlayer? outputDevice;
        private AudioFileReader? audioFile;

        private ISampleProvider? baseProvider;
        private ISampleProvider? currentProvider;

        // 各エフェクトを保持
        private MultiTapEchoProvider? echo;
        private HighQualityReverbProvider? reverb;
        private HighQualityTremoloProvider? tremolo;
        private SimpleChorusProvider? chorus;
        private SimpleCompressorProvider? compressor;

        private bool isEchoEnabled = false;
        private bool isReverbEnabled = false;
        private bool isTremoloEnabled = false;
        private bool isChorusEnabled = false;
        private bool isCompressorEnabled = false;

        public EffectWindow()
        {
            InitializeComponent();
        }

        private void SelectAndPlayButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "MP3 Files|*.mp3|WAV Files|*.wav",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                PlayAudio(dialog.FileName);
            }
        }

        private void PlayAudio(string path)
        {
            StopAudio();
            audioFile = new AudioFileReader(path);
            baseProvider = audioFile;

            BuildInitialChain();

            outputDevice = new WaveOutEvent() { DesiredLatency = 150 };
            outputDevice.Init(currentProvider);
            outputDevice.Play();
        }

        private void StopAudio()
        {
            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;

            audioFile?.Dispose();
            audioFile = null;
        }

        private void BuildInitialChain()
        {
            if (baseProvider == null) return;

            ISampleProvider provider = baseProvider;

            echo = new MultiTapEchoProvider(provider);
            reverb = new HighQualityReverbProvider(echo);
            tremolo = new HighQualityTremoloProvider(reverb);
            chorus = new SimpleChorusProvider(tremolo);
            compressor = new SimpleCompressorProvider(chorus);

            currentProvider = compressor;
        }

        private void RefreshEffectParameters()
        {
            if (baseProvider == null) return;

            float echoLevel = (float)(1.0 - EchoSlider.Value);
            float reverbLevel = (float)(1.0 - ReverbSlider.Value);
            float tremoloLevel = (float)(1.0 - TremoloDepthSlider.Value);
            float chorusLevel = (float)(1.0 - ChorusSlider.Value);
            float compressorLevel = (float)(1.0 - CompressorSlider.Value);

            echo?.UpdateParameters(isEchoEnabled, echoLevel);
            reverb?.UpdateParameters(isReverbEnabled, reverbLevel);
            tremolo?.UpdateParameters(isTremoloEnabled, tremoloLevel);
            chorus?.UpdateParameters(isChorusEnabled, chorusLevel);
            compressor?.UpdateParameters(isCompressorEnabled, compressorLevel);
        }

        // トグルボタン
        private void ToggleEchoButton_Click(object sender, RoutedEventArgs e) { isEchoEnabled = !isEchoEnabled; RefreshEffectParameters(); }
        private void ToggleReverbButton_Click(object sender, RoutedEventArgs e) { isReverbEnabled = !isReverbEnabled; RefreshEffectParameters(); }
        private void ToggleTremoloButton_Click(object sender, RoutedEventArgs e) { isTremoloEnabled = !isTremoloEnabled; RefreshEffectParameters(); }
        private void ToggleChorusButton_Click(object sender, RoutedEventArgs e) { isChorusEnabled = !isChorusEnabled; RefreshEffectParameters(); }
        private void ToggleCompressorButton_Click(object sender, RoutedEventArgs e) { isCompressorEnabled = !isCompressorEnabled; RefreshEffectParameters(); }

        // スライダー変更イベント
        private void EchoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshEffectParameters();
        private void ReverbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshEffectParameters();
        private void TremoloDepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshEffectParameters();
        private void ChorusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshEffectParameters();
        private void CompressorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshEffectParameters();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopAudio();
        }

        // ==============================
        // 各エフェクトクラス
        // ==============================

        public class MultiTapEchoProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private readonly float[] buffer;
            private int position;
            private float feedback = 0.4f;
            private float mix = 0f;
            private bool enabled = false;

            public MultiTapEchoProvider(ISampleProvider source)
            {
                this.source = source;
                WaveFormat = source.WaveFormat;
                int delaySamples = (int)(0.35 * WaveFormat.SampleRate);
                buffer = new float[delaySamples * WaveFormat.Channels];
            }

            public WaveFormat WaveFormat { get; }

            public void UpdateParameters(bool enabled, float level)
            {
                this.enabled = enabled;
                mix = enabled ? level * 0.4f : 0f;
            }

            public int Read(float[] bufferOut, int offset, int count)
            {
                int read = source.Read(bufferOut, offset, count);
                if (!enabled || mix <= 0.001f) return read;

                for (int i = 0; i < read; i++)
                {
                    float input = bufferOut[offset + i];
                    float delayed = buffer[position];
                    bufferOut[offset + i] = input * (1 - mix) + delayed * mix;
                    buffer[position] = input + delayed * feedback;
                    position++;
                    if (position >= buffer.Length) position = 0;
                }
                return read;
            }
        }

        public class HighQualityReverbProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private readonly float[] buffer;
            private int position;
            private float wet = 0f;
            private bool enabled = false;

            public HighQualityReverbProvider(ISampleProvider source)
            {
                this.source = source;
                WaveFormat = source.WaveFormat;
                buffer = new float[(int)(0.3 * WaveFormat.SampleRate) * WaveFormat.Channels];
            }

            public WaveFormat WaveFormat { get; }

            public void UpdateParameters(bool enabled, float level)
            {
                this.enabled = enabled;
                wet = enabled ? 0.2f + level * 0.3f : 0f;
            }

            public int Read(float[] bufferOut, int offset, int count)
            {
                int read = source.Read(bufferOut, offset, count);
                if (!enabled || wet <= 0.001f) return read;

                for (int i = 0; i < read; i++)
                {
                    float input = bufferOut[offset + i];
                    float delayed = buffer[position];
                    bufferOut[offset + i] = input * (1 - wet) + delayed * wet;
                    buffer[position] = input + delayed * 0.5f;
                    position++;
                    if (position >= buffer.Length) position = 0;
                }
                return read;
            }
        }

        public class HighQualityTremoloProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private double rate = 4.0;
            private float depth = 0f;
            private bool enabled = false;
            private int sample = 0;

            public HighQualityTremoloProvider(ISampleProvider source)
            {
                this.source = source;
                WaveFormat = source.WaveFormat;
            }

            public WaveFormat WaveFormat { get; }

            public void UpdateParameters(bool enabled, float level)
            {
                this.enabled = enabled;
                depth = enabled ? 0.3f + level * 0.4f : 0f;
                rate = 2.0 + level * 4.0;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int read = source.Read(buffer, offset, count);
                if (!enabled || depth <= 0.001f) return read;

                double samplesPerCycle = WaveFormat.SampleRate / rate;
                for (int n = 0; n < read; n++)
                {
                    double mod = 1 - (depth * 0.5 * (1 + Math.Sin(2 * Math.PI * sample / samplesPerCycle)));
                    buffer[offset + n] *= (float)mod;
                    sample++;
                }
                return read;
            }
        }

        public class SimpleChorusProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private readonly float[] buffer;
            private int position;
            private bool enabled = false;
            private float depth = 0f;
            private int sampleCount;
            private readonly float lfoRate = 0.25f;

            public SimpleChorusProvider(ISampleProvider source)
            {
                this.source = source;
                WaveFormat = source.WaveFormat;
                buffer = new float[(int)(0.04 * WaveFormat.SampleRate) * WaveFormat.Channels];
            }

            public WaveFormat WaveFormat { get; }

            public void UpdateParameters(bool enabled, float level)
            {
                this.enabled = enabled;
                depth = enabled ? 0.02f + level * 0.04f : 0f;
            }

            public int Read(float[] buf, int offset, int count)
            {
                int read = source.Read(buf, offset, count);
                if (!enabled || depth <= 0.001f) return read;

                for (int n = 0; n < read; n++)
                {
                    float input = buf[offset + n];
                    double lfo = (1 + Math.Sin(2 * Math.PI * lfoRate * sampleCount / WaveFormat.SampleRate)) / 2;
                    int delay = (int)(lfo * depth * buffer.Length);
                    int readPos = (position - delay + buffer.Length) % buffer.Length;
                    float delayed = buffer[readPos];
                    buf[offset + n] = input * 0.8f + delayed * 0.2f;
                    buffer[position] = input;
                    position = (position + 1) % buffer.Length;
                    sampleCount++;
                }
                return read;
            }
        }

        public class SimpleCompressorProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private float threshold = 1f;
            private bool enabled = false;

            public SimpleCompressorProvider(ISampleProvider source)
            {
                this.source = source;
                WaveFormat = source.WaveFormat;
            }

            public WaveFormat WaveFormat { get; }

            public void UpdateParameters(bool enabled, float level)
            {
                this.enabled = enabled;
                threshold = enabled ? 0.5f + level * 0.4f : 1f;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int read = source.Read(buffer, offset, count);
                if (!enabled || threshold >= 0.99f) return read;

                for (int i = 0; i < read; i++)
                {
                    float s = buffer[offset + i];
                    float a = Math.Abs(s);
                    if (a > threshold)
                        buffer[offset + i] = Math.Sign(s) * (threshold + (a - threshold) * 0.3f);
                }
                return read;
            }
        }
    }
}
