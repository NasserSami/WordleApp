using Grpc.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WordServer.Protos;

namespace WordServer.Services
{
    public class WordServerService : DailyWord.DailyWordBase
    {
        private readonly ILogger<WordServerService> _logger;
        private const string FileName = "wordle.json";
        private static string[] _words;

        public WordServerService( ILogger<WordServerService> logger )
        {
            _logger = logger;
        }

        public static string GetWord()
        {
            try
            {
                var json = File.ReadAllText(FileName);
                _words = JsonConvert.DeserializeObject<string[]>(json)!;
            }
            catch (IOException e)
            {
                Console.WriteLine("Error reading file!: " + e.Message);
            }
            catch (JsonException e)
            {
                Console.WriteLine("Error parsing JSON!: " + e.Message);
            }

            int seed = (DateTime.Now.Year * 10000) + (DateTime.Now.Month * 100) + DateTime.Now.Day;
            Random random = new Random(seed);
            return _words[random.Next(0, _words.Length)];
        }

    }
}
