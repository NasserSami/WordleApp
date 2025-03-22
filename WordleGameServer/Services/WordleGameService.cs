using Grpc.Core;
using WordleGameServer.Client;
using WordleGameServer.Protos;

namespace WordleGameServer.Services
{
    public class WordleGameService : DailyWordle.DailyWordleBase
    {

        private readonly ILogger<WordleGameService> _logger;

        public WordleGameService( ILogger<WordleGameService> logger )
        {
            _logger = logger;
        }

        public override async Task Play( IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context )
        {
            _logger.LogInformation("Starting game");
            var word = WordleGameServiceClient.GetWord();
            var response = new GuessResponse();
            response.GuessesLeft = 6;

            while (await requestStream.MoveNext())
            {
                var guess = requestStream.Current.Guess;
                if (!WordleGameServiceClient.ValidateWord(guess))
                {
                    response.IsCorrect = false;
                    await responseStream.WriteAsync(response);
                    continue;
                }
                if (guess == word)
                {
                    response.IsCorrect = true;
                    await responseStream.WriteAsync(response);
                    break;
                }
                else
                {
                    var correctLetters = word.Where(( c, i ) => guess.Length > i && guess[i] == c).Count();
                    var misplacedLetters = guess.Where(( c, i ) => word.Length > i && word[i] != c && word.Contains(c)).Count();
                    //response.Guesses.Add($"{guess} - {correctLetters} correct, {misplacedLetters} misplaced");
                    await responseStream.WriteAsync(response);
                }
            }
        }


        //public 

    }
}
