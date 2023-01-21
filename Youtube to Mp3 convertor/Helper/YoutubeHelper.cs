using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;

namespace Youtube_to_Mp3_convertor.Helper
{
    public class YoutubeHelper
    {
        private readonly YoutubeClient _youtube;
        public YoutubeHelper()
        {
            _youtube = new YoutubeClient();
        }
        public bool IsValidLink(string link)
        {
            if (link == null)
            {
                return false;
            }

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
            return await _youtube.Videos.GetAsync(link);
        }

        public async Task<IStreamInfo> GetAudioStreamAsync(string link)
        {
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(link);

            // ...or highest bitrate audio-only stream
            return streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        }

        public async Task DownloadAudioAsync(IStreamInfo streamInfo, string filename)
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "audio");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filepath = Path.Combine(directory,
                                        $"{RemoveInvalidFileNameChars(filename)}.{streamInfo.Container}");

            try
            {
                // check if the video has a audio stream
                if (streamInfo == null)
                {
                    throw new Exception("No audio stream available in the video.");
                }
                await _youtube.Videos.Streams.DownloadAsync(streamInfo, filepath);
            }
            catch (Exception ex)
            {
                throw new Exception("Youtube explode exception while downloading");
            }

        }


        public string RemoveInvalidFileNameChars(string fileName)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format("[{0}]+", invalidChars);
            return Regex.Replace(fileName, invalidRegStr, "_");
        }
    }
}
