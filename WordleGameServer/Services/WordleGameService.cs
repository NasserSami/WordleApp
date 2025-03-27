using Grpc.Core;
using WordleGameServer.Client;
using WordleGameServer.Models;
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

        /* ------------------------------ gRPC methods ------------------------------ */
        public override async Task Play( IAsyncStreamReader<GuessRequest> requestStream,
                                      IServerStreamWriter<GuessResponse> responseStream,
                                      ServerCallContext context )
        {
            _logger.LogInformation("Starting a new Wordle game");

            // Get the daily word from WordServer
            string wordToGuess = WordleGameServiceClient.GetWord();

            if (string.IsNullOrEmpty(wordToGuess))
            {
                _logger.LogError("Failed to get word from WordServer");
                await responseStream.WriteAsync(new GuessResponse
                {
                    IsGameOver = true,
                    GuessesLeft = 0
                });
                return;
            }

            _logger.LogInformation($"Daily word is: {wordToGuess}");

            // Create a new game with the word to guess
            var game = new WordleGame(wordToGuess);
            var hasWon = false;

            try
            {
                // Process guesses as they come in
                while (await requestStream.MoveNext() && !game.IsGameOver)
                {
                    var guessRequest = requestStream.Current;
                    string guess = guessRequest.Guess?.Trim().ToLower() ?? "";

                    _logger.LogInformation($"Received guess: {guess}");

                    // Validate the guess with WordServer if it's the right length
                    bool isValidWord = guess.Length == 5 && WordleGameServiceClient.ValidateWord(guess);

                    GuessResponse response;

                    if (!isValidWord)
                    {
                        // Invalid word - don't count as a guess
                        response = new GuessResponse
                        {
                            IsValidWord = false,
                            GuessesLeft = (uint)game.GuessesRemaining,
                            IsGameOver = game.IsGameOver,
                            IsCorrect = false
                        };

                        _logger.LogInformation($"Invalid word: {guess}");
                    }
                    else
                    {
                        // Process valid guess
                        response = game.ProcessGuess(guess);

                        if (response.IsCorrect)
                        {
                            hasWon = true;
                            _logger.LogInformation($"Player guessed the word in {6 - game.GuessesRemaining} attempts");
                        }
                    }

                    // Send response back to the client
                    await responseStream.WriteAsync(response);

                    // End the game if it's over
                    if (game.IsGameOver)
                    {
                        _logger.LogInformation($"Game over. Player {(hasWon ? "won" : "lost")}");
                        break;
                    }
                }

                // Update stats at the end of the game
                var stats = GameStats.GetCurrentStats();
                stats.AddGameResult(hasWon, 6 - game.GuessesRemaining);

                _logger.LogInformation("Game completed and stats updated");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during game: {ex.Message}");
            }
        }

        public override Task<StatsResponse> GetStats( StatsRequest request, ServerCallContext context )
        {
            _logger.LogInformation("Getting game stats");

            var stats = GameStats.GetCurrentStats();

            return Task.FromResult(new StatsResponse
            {
                Players = (uint)stats.TotalPlayers,
                WinnerPercentage = stats.GetWinPercentage(),
                AverageGuesses = stats.GetAverageGuesses()
            });
        }
    }
}