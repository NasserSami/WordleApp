using Newtonsoft.Json;

namespace WordleGameClient
{
    public class WordServer
    {
        private const string FileName = "wordle.json";
        private static readonly WordServer[] _words;

        static WordServer()
        {
            try
            {
               var json = File.ReadAllText(FileName);
               _words = JsonConvert.DeserializeObject<WordServer[]>(json)!;
            }
            catch (IOException e)
            {
                Console.WriteLine("Error reading file!");
            }


        }

        public string GetWord()
        {





            return "Hello, World!";
        }

    }
}
