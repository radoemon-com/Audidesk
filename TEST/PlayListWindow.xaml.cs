using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json;

namespace Audidesk
{
    public partial class PlayListWindow : Window
    {
        public PlayListWindow()
        {
            InitializeComponent();
            LoadConfig();
            SaveMusicList();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(@"D:\Softwere\Audidesk\TEST\Conf.ini");

                string musicPath = data["Paths"]["musicPath"];
                if (string.IsNullOrWhiteSpace(musicPath))
                {
                    MessageBox.Show("INIファイルのmusicPathが無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                musicPath = Path.GetFullPath(musicPath);
                if (!Directory.Exists(musicPath))
                {
                    Directory.CreateDirectory(musicPath);
                }

                Console.WriteLine("Current Directory: " + musicPath);

                TEST.Text = musicPath;

                var dialog = new OpenFileDialog
                {
                    Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav",
                    Multiselect = true,
                    Title = "音楽ファイルを選択"
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destinationPath = Path.Combine(musicPath, fileName);

                        File.Copy(filePath, destinationPath, true);
                        Console.WriteLine($"Copied: {filePath} -> {destinationPath}");
                    }

                    TEST.Text = $"ファイルを {dialog.FileNames.Length} 件インポートしました。";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました:\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SaveMusicList();
        }

        public static void LoadConfig()
        {
            try
            {
                var parser = new FileIniDataParser();
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Conf.ini");

                Console.WriteLine("Config loaded from: " + configPath);

                IniData data = parser.ReadFile(configPath);

                // データを使って設定を適用する処理をここに追加
                MessageBox.Show($"{configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("設定ファイルの読み込みに失敗しました: " + ex.Message);
            }
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(@"D:\Softwere\Audidesk\TEST\Conf.ini");

            string playListName = PlayListNameTextBox.Text;

            var alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var randomCode = new char[8];
            var random = new Random();

            string playListPath = data["Paths"]["playListPath"];
            if (string.IsNullOrWhiteSpace(playListPath))
            {
                MessageBox.Show("INIファイルのplayListPathが無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            playListPath = Path.GetFullPath(playListPath);
            if (!Directory.Exists(playListPath))
            {
                Directory.CreateDirectory(playListPath);
            }

            for (int i = 0; i < randomCode.Length; i++)
            {
                randomCode[i] = alphanumeric[random.Next(alphanumeric.Length)];
            }

            var fileName = new String(randomCode) + ".json";
            var fullPath = Path.Combine(playListPath, fileName);

            try
            {
                var playlist = new
                {
                    playList = new[]
                    {
                        new {
                            name = $"{playListName}",
                            music = new
                            {}
                        }
                    }
                };

                string jsonString = JsonConvert.SerializeObject(playlist, Formatting.Indented);

                File.WriteAllText(fullPath, jsonString); // ← ここで内容を書き込む

                Console.WriteLine("Create File: " + fullPath);
                MessageBox.Show("プレイリストファイルを作成しました:\n" + fullPath, "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました:\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMusicList()
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(@"D:\Softwere\Audidesk\TEST\Conf.ini");
            string musicListPath = @"D:\Softwere\Audidesk\TEST\TEST_PlayList\musicList.json";

            string musicPath = data["Paths"]["musicPath"];
            musicPath = Path.GetFullPath(musicPath);
            if (!Directory.Exists(musicPath))
            {
                Directory.CreateDirectory(musicPath);
            }

            var files = Directory.GetFiles(musicPath, "*.*")
                .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var musicDict = new Dictionary<string, object>();
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                musicDict[i.ToString()] = new
                {
                    filePath = $"{musicPath}\\{fileName}",
                    title = Path.GetFileNameWithoutExtension(fileName),
                };
            }

            var musicList = new
            {
                musicList = new
                {
                    name = "musicList",
                    music = musicDict
                }
            };

            string json = JsonConvert.SerializeObject(musicList, Formatting.Indented);
            File.WriteAllText(musicListPath, json);
        }
    }
}
