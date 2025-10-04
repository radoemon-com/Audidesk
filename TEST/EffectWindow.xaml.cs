using System;
using System.Windows;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audidesk
{
    public partial class EffectWindow : Window
    {
        private IWavePlayer? outputDevice;
        private AudioFileReader? audioFile;

        private SimpleEchoProvider? echoProvider;
        private SimpleReverbProvider? reverbProvider;

        private ISampleProvider? currentProvider;
        private ISampleProvider? baseProvider;

        private bool isEchoEnabled = false;
        private bool isReverbEnabled = false;

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

            UpdateEffectChain();

            outputDevice = new WaveOutEvent();
            outputDevice.Init(currentProvider);
            outputDevice.Play();
        }

        private void StopAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            audioFile?.Dispose();
            audioFile = null;
            echoProvider = null;
            reverbProvider = null;
            baseProvider = null;
            currentProvider = null;
        }

        private void UpdateEffectChain()
        {
            ISampleProvider provider = baseProvider!;

            if (isEchoEnabled)
            {
                echoProvider = new SimpleEchoProvider(provider, TimeSpan.FromMilliseconds(300), (float)EchoSlider.Value);
                provider = echoProvider;
            }
            else
            {
                echoProvider = null;
            }

            if (isReverbEnabled)
            {
                reverbProvider = new SimpleReverbProvider(provider, TimeSpan.FromMilliseconds(300), (float)ReverbSlider.Value);
                provider = reverbProvider;
            }
            else
            {
                reverbProvider = null;
            }

            currentProvider = provider;

            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Stop();
                outputDevice.Init(currentProvider);
                outputDevice.Play();
            }
        }

        private void ToggleEchoButton_Click(object sender, RoutedEventArgs e)
        {
            isEchoEnabled = !isEchoEnabled;
            UpdateEffectChain();
            MessageBox.Show($"Echo {(isEchoEnabled ? "ON" : "OFF")}");
        }

        private void ToggleReverbButton_Click(object sender, RoutedEventArgs e)
        {
            isReverbEnabled = !isReverbEnabled;
            UpdateEffectChain();
            MessageBox.Show($"Reverb {(isReverbEnabled ? "ON" : "OFF")}");
        }

        private void EchoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (echoProvider != null)
            {
                echoProvider.Feedback = (float)e.NewValue;
            }
        }

        private void ReverbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (reverbProvider != null)
            {
                reverbProvider.ReverbAmount = (float)e.NewValue;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopAudio();
        }
    }

    // エコー効果用シンプルプロバイダー
    public class SimpleEchoProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] buffer;
        private int bufferOffset;
        private readonly int delaySamples;
        private float decay;

        public SimpleEchoProvider(ISampleProvider source, TimeSpan delay, float decay)
        {
            this.source = source;
            this.decay = decay;
            WaveFormat = source.WaveFormat;

            delaySamples = (int)(delay.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
            buffer = new float[delaySamples];
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] outputBuffer, int offset, int count)
        {
            int read = source.Read(outputBuffer, offset, count);

            for (int n = 0; n < read; n++)
            {
                float delayed = buffer[bufferOffset];
                buffer[bufferOffset] = outputBuffer[offset + n] + delayed * decay;
                outputBuffer[offset + n] += delayed;

                bufferOffset++;
                if (bufferOffset >= buffer.Length)
                    bufferOffset = 0;
            }

            return read;
        }

        public float Feedback
        {
            get => decay;
            set => decay = value;
        }
    }

    // リバーブ効果用シンプルプロバイダー
    public class SimpleReverbProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] delayBuffer;
        private int delayOffset;
        private float reverbLevel;

        public SimpleReverbProvider(ISampleProvider source, TimeSpan delay, float reverbLevel)
        {
            this.source = source;
            this.reverbLevel = reverbLevel;
            WaveFormat = source.WaveFormat;

            int delaySamples = (int)(delay.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
            delayBuffer = new float[delaySamples];
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);

            for (int i = 0; i < read; i++)
            {
                float delayed = delayBuffer[delayOffset];
                delayBuffer[delayOffset] = buffer[offset + i] + delayed * reverbLevel;
                buffer[offset + i] += delayed * 0.5f;

                delayOffset++;
                if (delayOffset >= delayBuffer.Length)
                    delayOffset = 0;
            }

            return read;
        }

        public float ReverbAmount
        {
            get => reverbLevel;
            set => reverbLevel = value;
        }
    }
}
