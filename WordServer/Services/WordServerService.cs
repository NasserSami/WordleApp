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
        private static string[] _words = [];

        static WordServerService()
        {
            LoadWords();
        }

        public WordServerService( ILogger<WordServerService> logger )
        {
            _logger = logger;
        }

        /* ------------------------------ gRPC methods ------------------------------ */
        public override Task<WordResponse> GetWord( WordRequest request, ServerCallContext context )
        {
            _logger.LogInformation("Getting daily word");
            return Task.FromResult(new WordResponse { Word = GetDailyWord() });
        }

        public override Task<ValidateWordResponse> ValidateWord( ValidateWordRequest request, ServerCallContext context )
        {
            _logger.LogInformation("Validating word");
            return Task.FromResult(new ValidateWordResponse { IsValid = WordValidation(request.Word) });
        }

        /* ------------------------------ Helper methods ------------------------------ */
        private string GetDailyWord()
        {
            // Generate a seed based on the current day
            int seed = (DateTime.Now.Year * 10000) + (DateTime.Now.Month * 100) + DateTime.Now.Day;
            Random random = new Random(seed);

            if (_words.Length == 0)
            {
                _logger.LogError("Word list is empty!");
                return "ERROR: Words Load Error ";
            }

            return _words[random.Next(0, _words.Length)];
        }

        private bool WordValidation( string word )
        {
            return _words.Contains(word);
        }

        private static void LoadWords()
        {
            try
            {
                var json = File.ReadAllText(FileName);
                _words = JsonConvert.DeserializeObject<string[]>(json) ?? [];

                if (_words.Length == 0)
                    Console.WriteLine("Warning: No words were loaded from the JSON file");
                else
                    Console.WriteLine($"Successfully loaded {_words.Length} words");
            }
            catch (IOException e)
            {
                Console.WriteLine("Error reading file: " + e.Message);
                // Initialize to empty array to prevent null reference exceptions
                _words = [];
            }
            catch (JsonException e)
            {
                Console.WriteLine("Error parsing JSON: " + e.Message);
                _words = [];
            }
        }
    }
}
