using Grpc.Core;
using Grpc.Net.Client;
using System.Text;
using WordleGameServer.Protos;

namespace WordleGameClient
{
    internal class Program
    {
        // Define colors for the different result types
        private static readonly Dictionary<ResultType, ConsoleColor> ResultColors = new Dictionary<ResultType, ConsoleColor>
        {
            { ResultType.NotInWord, ConsoleColor.DarkGray },
            { ResultType.WrongPosition, ConsoleColor.Yellow },
            { ResultType.CorrectPosition, ConsoleColor.Green }
        };

        // Main entry point for the Wordle game client
        static async Task Main( string[] args )
        {
            DrawTitle();

            try
            {
                // Create client connection to the WordleGameServer
                using var channel = GrpcChannel.ForAddress("https://localhost:7018");
                var client = new DailyWordle.DailyWordleClient(channel);

                DrawInstructions();

                await PlayGame(client);
            }
            catch (RpcException)
            {
                Console.WriteLine("Error: Wordle Game Server is currently unavailable!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        // Play the Wordle game
        private static async Task PlayGame( DailyWordle.DailyWordleClient client )
        {
            using var call = client.Play();

            // Keep track of what letters have been guessed
            HashSet<string> availableLetters = new HashSet<string>("abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString()));
            HashSet<string> includedLetters = new HashSet<string>();
            HashSet<string> excludedLetters = new HashSet<string>();

            // Play until game is over
            int guessNumber = 1;
            bool gameOver = false;
            bool gameWon = false;

            while (!gameOver)
            {
                // Display current letter status
                DisplayLetterStatus(availableLetters, includedLetters, excludedLetters);


                // Get user guess
                Console.Write($"\n({guessNumber}): ");
                string guess = Console.ReadLine()?.Trim().ToLower() ?? "";

                // Send guess to server
                await call.RequestStream.WriteAsync(new GuessRequest { Guess = guess });

                // Wait for response
                if (!await call.ResponseStream.MoveNext())
                {
                    Console.WriteLine("Lost connection to server.");
                    break;
                }

                // Process response
                var response = call.ResponseStream.Current;

                if (!response.IsValidWord)
                {
                    Console.WriteLine("Not a valid word. Try again.");
                    continue;
                }

                // Display the results of the guess
                DisplayGuessResult(response.LetterResults);

                // Update the letter statuses
                UpdateLetters(response, ref availableLetters, ref includedLetters, ref excludedLetters);

                // Check game status
                gameOver = response.IsGameOver;
                gameWon = response.IsCorrect;

                if (gameWon)
                {
                    Console.WriteLine("\nYou win!");
                }
                else if (gameOver)
                {
                    Console.WriteLine("\nGame over! You've used all your guesses.");
                }

                guessNumber++;
            }

            // Complete the request stream
            await call.RequestStream.CompleteAsync();

            // Show game statistics
            if (gameOver)
            {
                await DisplayGameStatistics(client);
            }
        }

        // Display the results of the guess
        private static void DisplayGuessResult( IEnumerable<LetterResult> letterResults )
        {
            var defaultColor = Console.ForegroundColor;

            // Display the results for each letter with colors
            Console.Write("     ");
            foreach (var result in letterResults)
            {
                Console.ForegroundColor = ResultColors[result.Result];
                Console.Write(result.Letter.ToUpper());
                Console.ForegroundColor = defaultColor;
                Console.Write(" ");
            }
            Console.WriteLine();

            Console.ForegroundColor = defaultColor;
        }

        // Update the available, included, and excluded letters based on the server response
        private static void UpdateLetters( GuessResponse response,
                                         ref HashSet<string> availableLetters,
                                         ref HashSet<string> includedLetters,
                                         ref HashSet<string> excludedLetters )
        {
            availableLetters = new HashSet<string>(response.AvailableLetters);
            includedLetters = new HashSet<string>(response.IncludedLetters);
            excludedLetters = new HashSet<string>(response.ExcludedLetters);
        }

        // Display the current status of available, included, and excluded letters
        private static void DisplayLetterStatus( HashSet<string> availableLetters,
                                              HashSet<string> includedLetters,
                                              HashSet<string> excludedLetters )
        {
            var defaultColor = Console.ForegroundColor;

            // Display available letters
            Console.Write("\nAvailable: ");
            foreach (var letter in "abcdefghijklmnopqrstuvwxyz")
            {
                string letterStr = letter.ToString();
                if (availableLetters.Contains(letterStr))
                {
                    Console.ForegroundColor = defaultColor;
                    Console.Write(letterStr);
                    if (letterStr != "z") Console.Write(",");
                }
            }
            Console.WriteLine();

            // Display included letters
            if (includedLetters.Any())
            {
                Console.Write("Included: ");
                foreach (var letter in includedLetters)
                {
                    Console.Write(letter);
                    if (letter != includedLetters.Last()) Console.Write(",");
                }
                Console.WriteLine();
            }

            // Display excluded letters
            if (excludedLetters.Any())
            {
                Console.ForegroundColor = defaultColor;
                Console.Write("Excluded: ");
                foreach (var letter in excludedLetters)
                {
                    Console.Write(letter);
                    if (letter != excludedLetters.Last()) Console.Write(",");
                }
                Console.WriteLine();
            }

        }

        // Display the game statistics
        private static async Task DisplayGameStatistics( DailyWordle.DailyWordleClient client )
        {
            try
            {
                var statsResponse = await client.GetStatsAsync(new StatsRequest());

                Console.WriteLine("\nStatistics");
                Console.WriteLine("---------");
                Console.WriteLine($"Players: {statsResponse.Players}");
                Console.WriteLine($"Winners: {statsResponse.WinnerPercentage}%");
                Console.WriteLine($"Average Guesses: {statsResponse.AverageGuesses}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnable to retrieve game statistics: {ex.Message}");
            }
        }

        // Draw the game title
        private static void DrawTitle()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            // ASCII art title for WORDLE
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
 _    _  ___  ____  ____  _     _____
| |  | |/ _ \|  _ \|  _ \| |   | ____|
| |/\| | | | | |_) | | | | |   |  _| 
|  /\  | |_| |  _ <| |_| | |___| |___
|_/  \_|\___/|_| \_\____/|_____|_____|
");
            Console.ResetColor();
        }

        // Draw the game instructions
        private static void DrawInstructions()
        {
            var defaultColor = Console.ForegroundColor;

            Console.WriteLine("\nYou have 6 chances to guess a 5-letter word.");
            Console.WriteLine("Each guess must be a 'playable' 5 letter word.");
            Console.WriteLine("\nAfter a guess the game will display letters in different colors:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("GRAY");
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(" - means the letter is not in the word.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("YELLOW");
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(" - means the letter is in the word but in the wrong position.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("GREEN");
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(" - means the letter is in the correct position.");
        }
    }
}