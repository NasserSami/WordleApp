using System.Text.Json;

namespace WordleGameServer.Models
{
    //Model class so that we can store and retrieve game statistics from a file
    //Better to have separate class for this instead of adding to WordleGame class
    public class GameStats
    {
        private static readonly string StatsFileName = "game_stats.json";
        private static readonly Mutex StatsMutex = new Mutex(false, "WordleGameStatsMutex");
        public DateTime LastResetDate { get; set; } = DateTime.Today;

        public int TotalPlayers { get; set; } = 0;
        public int TotalWinners { get; set; } = 0;
        public Dictionary<int, int> GuessCounts { get; set; } = new Dictionary<int, int>
        {
            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }
        };

        public static GameStats GetCurrentStats()
        {
            GameStats stats;

            try
            {
                StatsMutex.WaitOne();

                if (File.Exists(StatsFileName))
                {
                    string json = File.ReadAllText(StatsFileName);
                    stats = JsonSerializer.Deserialize<GameStats>(json) ?? new GameStats();
                }
                else
                {
                    stats = new GameStats();
                }

                // Check if we need to reset statistics (day changed)
                if (DateTime.Today > stats.LastResetDate)
                {
                    stats = new GameStats(); // Create a new stats object with today's date
                    stats.SaveStats();       // Save immediately to persist the new reset date
                }

                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stats: {ex.Message}");
                return new GameStats();
            }
            finally
            {
                StatsMutex.ReleaseMutex();
            }
        }

        public static void ResetStats()
        {
            try
            {
                StatsMutex.WaitOne();

                // Create new, empty stats with today's date
                var newStats = new GameStats
                {
                    LastResetDate = DateTime.Today
                };

                // Save to file
                string json = JsonSerializer.Serialize(newStats);
                File.WriteAllText(StatsFileName, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting stats: {ex.Message}");
            }
            finally
            {
                StatsMutex.ReleaseMutex();
            }
        }

        public void SaveStats()
        {
            try
            {
                StatsMutex.WaitOne();

                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(StatsFileName, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving stats: {ex.Message}");
            }
            finally
            {
                StatsMutex.ReleaseMutex();
            }
        }

        public void AddGameResult( bool won, int guessCount )
        {
            TotalPlayers++;

            if (won && guessCount >= 1 && guessCount <= 6)
            {
                TotalWinners++;
                GuessCounts[guessCount]++;
            }

            SaveStats();
        }

        public double GetWinPercentage()
        {
            if (TotalPlayers == 0)
                return 0;

            return Math.Round((double)TotalWinners / TotalPlayers * 100, 1);
        }

        public double GetAverageGuesses()
        {
            int totalGuesses = 0;
            int totalWinners = 0;

            for (int i = 1; i <= 6; i++)
            {
                totalGuesses += i * GuessCounts[i];
                totalWinners += GuessCounts[i];
            }

            if (totalWinners == 0)
                return 0;

            return Math.Round((double)totalGuesses / totalWinners, 1);
        }
    }
}