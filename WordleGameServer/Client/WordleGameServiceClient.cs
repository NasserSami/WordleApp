using Grpc.Net.Client;
using WordServer.Protos;

namespace WordleGameServer.Client
{
    public static class WordleGameServiceClient
    {
        private static DailyWord.DailyWordClient? _dailyWordServer = null;

        public static string GetWord()
        {
            ConnectToService();
            var wordRequest = new WordRequest();
            var wordResponse = _dailyWordServer?.GetWord(wordRequest);
            return wordResponse?.Word ?? "";
        }

        public static bool ValidateWord( string word )
        {
            ConnectToService();
            var validateWordRequest = new ValidateWordRequest { Word = word };
            var validateWordResponse = _dailyWordServer?.ValidateWord(validateWordRequest);
            return validateWordResponse?.IsValid ?? false;
        }

        private static void ConnectToService()
        {
            if (_dailyWordServer is null)
            {
                _dailyWordServer = new DailyWord.DailyWordClient
                    (GrpcChannel.ForAddress("https://localhost:7073"));
            }
        }
    }
}
