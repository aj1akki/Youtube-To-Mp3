using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;
using Microsoft.Security.Application;


namespace Youtube_to_Mp3_convertor.Helper
{
    public class YoutubeHelper
    {
        private readonly YoutubeClient _youtube;
        private readonly ILogger<YoutubeHelper> _logger;
        public YoutubeHelper(ILogger<YoutubeHelper> logger)
        {
            _youtube = new YoutubeClient();
            _logger = logger;
        }
        public bool IsValidLink(string link)
        {
            int maxYoutubeLength = 70;
            string[] blacklistedStrings = { "javascript:", "script", "eval", "onload" };

            if (link == null || link.Length > maxYoutubeLength)
            {
                return false;
            }

            foreach (string s in blacklistedStrings)
            {
                if (link.Contains(s))
                {
                    return false;
                }
            }
            link = Sanitizer.GetSafeHtmlFragment(link);

            //Regular expression to match youtube link
            string pattern = @"^(https?:\/\/)?(www\.)?(youtube\.com|youtu\.be)\/watch\?v=([\w-]{11})(&.*)?$";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(link);

            if (match.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public async Task<Video> GetVideoAsync(string link)
        {
            _logger.LogInformation("Log - GetVideoAsync request: Link:{0}", link);
            return await _youtube.Videos.GetAsync(link);
        }

        public async Task<IStreamInfo> GetAudioStreamAsync(string link)
        {
            _logger.LogInformation("Log - GetAudioStreamAsync request: Link:{0}", link);

            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(link);

            // ...or highest bitrate audio-only stream
            return streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        }

        public async Task DownloadAudioAsync(IStreamInfo streamInfo, string filename)
        {
            _logger.LogInformation("Log - DownloadAudioAsync request: StreamInfo:{0} Filename:{1}", streamInfo, filename);

            var directory = Path.Combine(Directory.GetCurrentDirectory(), "audio");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filepath = Path.Combine(directory, $"{RemoveInvalidFileNameChars(filename)}.{streamInfo.Container}");

            try
            {
                // check if the video has a audio stream
                if (streamInfo == null)
                {
                    throw new Exception("No audio stream available in the video.");
                }

                await _youtube.Videos.Streams.DownloadAsync(streamInfo, filepath);
                _logger.LogInformation("Log - DownloadAudioAsync successfull: StreamInfo:{0} Filename:{1}", streamInfo, filename);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Log - DownloadAudioAsync failed: StreamInfo:{0} Filename:{1}", streamInfo, filename);
                throw new Exception("Youtube explode exception while downloading");
            }

        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format("[{0}]+", invalidChars);
            return Regex.Replace(fileName, invalidRegStr, "_");
        }
    }
}
