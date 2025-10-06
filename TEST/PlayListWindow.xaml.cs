using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using IniParser;
using IniParser.Model;

namespace Audidesk
{
    public partial class PlayListWindow : Window
    {
        public PlayListWindow()
        {
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // INIファイルの読み込み
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(@"D:\Softwere\Audidesk\TEST\Conf.ini");

                // musicPathの取得と存在確認
                string musicPath = data["Paths"]["musicPath"];
                if (string.IsNullOrWhiteSpace(musicPath))
                {
                    MessageBox.Show("INIファイルのmusicPathが無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 絶対パスに変換（必要に応じて）
                musicPath = Path.GetFullPath(musicPath);
                if (!Directory.Exists(musicPath))
                {
                    Directory.CreateDirectory(musicPath); // なければ作成
                }

                // 現在のディレクトリをデバッグ出力
                Console.WriteLine("Current Directory: " + musicPath);

                TEST.Text = musicPath;

                // ファイル選択ダイアログの表示
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
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(@"D:\Softwere\Audidesk\TEST\Conf.ini");

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

            for (int i = 0; i < randomCode.Length; i++) {
                randomCode[i] = alphanumeric[random.Next(alphanumeric.Length)];
            }

            var fileName = new String(randomCode) + ".json";
            Console.WriteLine();

            try
            {
                // Jsonファイルの作成
                using (System.IO.FileStream fs = System.IO.File.Create(playListPath + "\\" + fileName))
                {
                    Console.WriteLine("Creat File: " + playListPath + "\\" + fileName);

                    
                }
            } catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました:\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
