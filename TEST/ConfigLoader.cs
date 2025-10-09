using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json;

namespace Audidesk
{
    public class ConfigLoader
    {
        private static IniData LoadConfig()
        {
            var parser = new FileIniDataParser();
            Console.WriteLine("Config loaded from: " + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Conf.ini"));
            return parser.ReadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Conf.ini"));
        }
    }
}