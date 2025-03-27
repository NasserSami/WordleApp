using WordleGameServer.Protos;

namespace WordleGameServer.Models
{
    //This class file helps us to create and manage a Wordle game
    //Better than having all the logic in the WordleGameService class
    public class WordleGame
    {
        public string WordToGuess { get; private set; }
        public int MaxGuesses { get; } = 6;
        public int GuessesRemaining { get; private set; }
        public bool IsGameOver => GuessesRemaining <= 0 || HasWon;
        public bool HasWon { get; private set; } = false;

        private HashSet<char> _availableLetters = new HashSet<char>("abcdefghijklmnopqrstuvwxyz");
        private HashSet<char> _includedLetters = new HashSet<char>();
        private HashSet<char> _excludedLetters = new HashSet<char>();

        public WordleGame( string wordToGuess )
        {
            WordToGuess = wordToGuess.ToLower();
            GuessesRemaining = MaxGuesses;
        }

        public GuessResponse ProcessGuess( string guess )
        {
            guess = guess.ToLower();

            var response = new GuessResponse
            {
                GuessesLeft = (uint)GuessesRemaining,
                IsGameOver = this.IsGameOver
            };

            // Validate length
            if (guess.Length != 5)
            {
                response.IsValidWord = false;
                return response;
            }

            // Game logic for a valid guess
            GuessesRemaining--;
            response.GuessesLeft = (uint)GuessesRemaining;
            response.IsValidWord = true;

            // Check if word is correct
            if (guess == WordToGuess)
            {
                HasWon = true;
                response.IsCorrect = true;
                response.IsGameOver = true;

                // All letters are in correct position
                foreach (char c in guess)
                {
                    _includedLetters.Add(c);
                    if (_availableLetters.Contains(c))
                        _availableLetters.Remove(c);
                }

                // Add all letters with CORRECT_POSITION result
                for (int i = 0; i < guess.Length; i++)
                {
                    response.LetterResults.Add(new LetterResult
                    {
                        Letter = guess[i].ToString(),
                        Result = ResultType.CorrectPosition
                    });
                }
            }
            else
            {
                response.IsCorrect = false;
                response.IsGameOver = GuessesRemaining <= 0;

                // Calculate letter frequency in the word to guess
                Dictionary<char, int> letterCounts = new Dictionary<char, int>();
                foreach (char c in WordToGuess)
                {
                    if (letterCounts.ContainsKey(c))
                        letterCounts[c]++;
                    else
                        letterCounts[c] = 1;
                }

                // First pass: mark correct positions
                LetterResult[] results = new LetterResult[guess.Length];

                for (int i = 0; i < guess.Length; i++)
                {
                    char guessChar = guess[i];

                    if (guessChar == WordToGuess[i])
                    {
                        results[i] = new LetterResult
                        {
                            Letter = guessChar.ToString(),
                            Result = ResultType.CorrectPosition
                        };

                        _includedLetters.Add(guessChar);
                        if (_availableLetters.Contains(guessChar))
                            _availableLetters.Remove(guessChar);

                        // Decrement the count since we've used this letter
                        letterCounts[guessChar]--;
                    }
                }

                // Second pass: check for wrong positions and not in word
                for (int i = 0; i < guess.Length; i++)
                {
                    if (results[i] != null) continue; // Skip already processed positions

                    char guessChar = guess[i];

                    if (letterCounts.ContainsKey(guessChar) && letterCounts[guessChar] > 0)
                    {
                        results[i] = new LetterResult
                        {
                            Letter = guessChar.ToString(),
                            Result = ResultType.WrongPosition
                        };

                        _includedLetters.Add(guessChar);
                        if (_availableLetters.Contains(guessChar))
                            _availableLetters.Remove(guessChar);

                        // Decrement the count since we've used this letter
                        letterCounts[guessChar]--;
                    }
                    else
                    {
                        results[i] = new LetterResult
                        {
                            Letter = guessChar.ToString(),
                            Result = ResultType.NotInWord
                        };

                        if (!_includedLetters.Contains(guessChar))
                        {
                            _excludedLetters.Add(guessChar);
                            if (_availableLetters.Contains(guessChar))
                                _availableLetters.Remove(guessChar);
                        }
                    }
                }

                // Add all results to the response
                foreach (var result in results)
                {
                    response.LetterResults.Add(result);
                }
            }

            // Add the letter status to the response
            response.AvailableLetters.AddRange(_availableLetters.Select(c => c.ToString()));
            response.IncludedLetters.AddRange(_includedLetters.Select(c => c.ToString()));
            response.ExcludedLetters.AddRange(_excludedLetters.Select(c => c.ToString()));

            return response;
        }
    }
}