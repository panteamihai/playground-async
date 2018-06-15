using AsyncWorkshop.UsagePatterns.Helpers;
using System;
using System.IO;
using System.Linq;

namespace AsyncWorkshop.UsagePatterns.Services
{
    public class MediaPathService : IMediaPathService
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

        public string Destination { get; } = Path.Combine(Path.GetTempPath(), "CopiedMediaForAsyncWorkshop");

        public void ClearDestination()
        {
            if (string.IsNullOrWhiteSpace(Destination) || !Directory.Exists(Destination))
                return;

            foreach (var file in Directory.GetFiles(Destination))
            {
                File.Delete(file);
            }
        }

        private static bool ContainsMusicFiles(string path)
        {
            return FileRetriever.GetFilePathsByPattern(path, "*.mp3|*.flac|*.m4a|*.ogg", SearchOption.AllDirectories).Any();
        }
    }
}
