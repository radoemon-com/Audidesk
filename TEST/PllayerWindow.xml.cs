using System;
using System.IO;
using System.Linq;
using System.Windows;
using NAudio.Wave;
using System.Collections.Generic;

namespace Audidesk
{
    public partial class PlayerWindow : Window
    {
        private IWavePlayer? outputDevice;
        private AudioFileReader? audioFile;
        private string[] mp3Files = Array.Empty<string>();
        private int currentTrackIndex = 0;

        private bool isShuffling = false;
        private Random random = new Random();

        public PlayerWindow()
        {
            InitializeComponent();
            Loaded += PlayerWindow_Loaded;
        }

        private void PlayerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMp3Files();
        }

        private void LoadMp3Files()
        {
            string musicFolder = Path.Combine(@"D:\Softwere\Audidesk\TEST\Music");

            if (!Directory.Exists(musicFolder))
            {
                MessageBox.Show("Music フォルダが見つかりません。");
                return;
            }

            mp3Files = Directory.GetFiles(musicFolder, "*.mp3");

            if (mp3Files.Length == 0)
            {
                MessageBox.Show("MP3 ファイルが見つかりません。");
                return;
            }

            MusicListBox.ItemsSource = mp3Files.Select(f => Path.GetFileName(f)).ToList();
        }

        private void MusicListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = MusicListBox.SelectedIndex;
            if (index < 0 || index >= mp3Files.Length)
                return;

            currentTrackIndex = index;
            PlayAudio(mp3Files[currentTrackIndex]);
        }

        private void PlayAudio(string path)
        {
            StopAudio();

            audioFile = new AudioFileReader(path);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.PlaybackStopped += OnPlaybackStopped;
            outputDevice.Play();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (isShuffling)
                {
                    // シャッフルモードならランダムに次を選ぶ（現在と違うものを選びたいなら工夫可能）
                    currentTrackIndex = random.Next(mp3Files.Length);
                }
                else
                {
                    // 通常の順番再生
                    currentTrackIndex++;
                    if (currentTrackIndex >= mp3Files.Length)
                        currentTrackIndex = 0; // ループ再生（止めたいなら return してもOK）
                }

                MusicListBox.SelectedIndex = currentTrackIndex;
                PlayAudio(mp3Files[currentTrackIndex]);
            });
        }

        private void StopAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.PlaybackStopped -= OnPlaybackStopped;
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            audioFile?.Dispose();
            audioFile = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopAudio();
        }

        // シャッフル切り替え用
        private void ToggleShuffle()
        {
            isShuffling = !isShuffling;
            MessageBox.Show($"シャッフル: {(isShuffling ? "ON" : "OFF")}");
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleShuffle();
        }
    }
}
