using AsyncWorkshop.UsagePatterns.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AsyncWorkshop.UsagePatterns.Services
{
    public class PathService : IPathService
    {
        private string _source = @"D:\Projects - Extra\Workshop\workshop-async\media";
        public string Source
        {
            get => _source;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Please provide a proper directory path!");

                if (!Directory.Exists(value))
                    throw new ArgumentException("No such directory!");

                if (!ContainsMusicFiles(value))
                    throw new ArgumentException("No music files in directory!");

                _source = value;
            }
        }

        public string Destination { get; } = Path.Combine(Environment.CurrentDirectory, "Copies");

        public string Utility { get; } = Path.Combine(Environment.CurrentDirectory, "Utility");

        public void ClearDestination()
        {
            if (string.IsNullOrWhiteSpace(Destination) || !Directory.Exists(Destination))
                return;

            foreach (var file in Directory.GetFiles(Destination))
            {
                File.Delete(file);
            }
        }

        public void ClearStandByList()
        {
            if (string.IsNullOrWhiteSpace(Utility) || !Directory.Exists(Utility))
                return;

            var utilityPath = Path.Combine(Utility, "EmptyStandbyList.exe");
            if (!File.Exists(utilityPath))
                return;

            Process.Start(utilityPath, "standbylist");
            Thread.Sleep(1000);
        }

        private static bool ContainsMusicFiles(string path)
        {
            return FileRetriever.GetFilePathsByPattern(path, "*.mp3|*.flac|*.m4a|*.ogg", SearchOption.AllDirectories).Any();
        }
    }
}
