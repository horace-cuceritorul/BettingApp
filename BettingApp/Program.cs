using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsiteParsers;

namespace BettingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeSpan delay = TimeSpan.FromMinutes(5);
            while (true)
            {
                try
                {
                    List<IWebsiteParser> parsers = new List<IWebsiteParser>() { new BetfairParser(), new WilliamHillParser() };
                    var matchOdds = new List<IEnumerable<MatchDetails>>();

                    foreach (var parser in parsers)
                    {
                        //Console.ForegroundColor = ConsoleColor.Green;
                        //Console.WriteLine("Maches for {0}", parser.GetType().Name);
                        //Console.ForegroundColor = ConsoleColor.White;
                        var matches = parser.ParseResultsForSport(new SportDetails { Name = "tennis" });

                        //foreach (var match in matches)
                        //{
                        //    Console.WriteLine("{0} ({1:0.00}) --- {2} ({3:0.00})", match.HomePlayer, match.HomeRate, match.AwayPlayer, match.AwayRate);
                        //}
                        //Console.WriteLine();
                        //Console.WriteLine();

                        matchOdds.Add(matches);
                    }

                    //Console.ForegroundColor = ConsoleColor.Blue;
                    //Console.WriteLine("Cumulated results:");
                    //Console.ForegroundColor = ConsoleColor.White;

                    Dictionary<string, List<MatchDetails>> existingMatches = new Dictionary<string, List<MatchDetails>>();
                    bool didOneRun = false;
                    foreach (var results in matchOdds)
                    {
                        foreach (var match in results)
                        {
                            var key = match.GetKey();
                            if (!existingMatches.ContainsKey(key))
                            {
                                if (didOneRun)
                                {
                                    string possibleKey = GetPossibleKeyMatch(key, existingMatches.Keys.ToList());
                                    if (possibleKey != null)
                                    {
                                        existingMatches[possibleKey].Add(match);
                                        continue;
                                    }
                                }
                                existingMatches.Add(key, new List<MatchDetails>() { match });
                            }
                            else
                            {
                                existingMatches[key].Add(match);
                            }
                        }
                        didOneRun = true;
                    }

                    decimal minBestRate = 10;
                    List<MatchDetails> minBestRateMatch = null;

                    foreach (var match in existingMatches)
                    {
                        //var homePlayer = match.Value[0].HomePlayer;
                        //var awayPlayer = match.Value[0].AwayPlayer;

                        //Console.Write("{0}( ", homePlayer);
                        //foreach (var matchDetail in match.Value)
                        //    Console.Write("{0:0.00} ", matchDetail.HomeRate);
                        //Console.Write(") --- ");

                        //Console.Write("{0}( ", awayPlayer);
                        //foreach (var matchDetail in match.Value)
                        //    Console.Write("{0:0.00} ", matchDetail.AwayRate);
                        //Console.Write(")    ");

                        var bestMoneyToMake = CalculateBestMoneyFor(match.Value);
                        //Console.Write("Best rate you can get is: ");
                        //if (bestMoneyToMake >= 1)
                        //    Console.ForegroundColor = ConsoleColor.Red;
                        //else
                        //    Console.ForegroundColor = ConsoleColor.Green;
                        //Console.WriteLine("{0:0.00}", bestMoneyToMake);
                        //Console.ForegroundColor = ConsoleColor.White;

                        if (bestMoneyToMake < minBestRate)
                        {
                            minBestRate = bestMoneyToMake;
                            minBestRateMatch = match.Value;
                        }
                    }
                    Console.WriteLine("{0:MM/dd H:mm:ss}", DateTime.Now);

                    File.AppendAllLines(ConfigurationManager.AppSettings["outputLocation"], new string[] {
                        string.Format("[{0:MM/dd H:mm:ss}]\t{3}The best rate found was {1:0.00} for the matches {2}", DateTime.Now, minBestRate, JsonConvert.SerializeObject(minBestRateMatch), minBestRate < 1 ? "   BINGO!!  " : "")
                    });
                }
                catch(AccessViolationException e)
                {
                    File.AppendAllLines(ConfigurationManager.AppSettings["outputLocation"], new string[] {
                        string.Format("[{0:MM/dd H:mm:ss}]\tThere was an error: {1}", DateTime.Now, e.Message)
                    });
                }
                Thread.Sleep(delay);
            }
        }

        /// <summary>
        /// Calculates the best rates if you bet at different partners
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static decimal CalculateBestMoneyFor(List<MatchDetails> list)
        {
            decimal bestRateHome = 0, bestRateAway = 0;
            foreach(var matchOdd in list)
            {
                if (matchOdd.HomeOdds > bestRateHome)
                    bestRateHome = matchOdd.HomeOdds;
                if (matchOdd.AwayOdds > bestRateAway)
                    bestRateAway = matchOdd.AwayOdds;
            }

            return 1 / bestRateHome + 1 / bestRateAway;
        }

        private static string GetPossibleKeyMatch(string key, List<string> existingKeys)
        {
            return existingKeys.FirstOrDefault(k => AreKeysEquivalent(k, key));
        }

        /// <summary>
        /// Parses a key to get full object details
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static dynamic ParseKeyAndGetDetails(string key)
        {
            //TODO first names with multiple names
            string[] splitted = key.Split('|');
            string[] player1splitted = splitted[0].Split(' '), player2splitted = splitted[0].Split(' ');
            dynamic player1 = new
            {
                lastName = player1splitted.Last(),
                firstName = player1splitted.First()
            }, player2 = new
            {
                lastName = player2splitted.Last(),
                firstName = player2splitted.First()
            };
            return new
            {
                player1,
                player2
            };
        }

        static bool AreKeysEquivalent(string key1, string key2)
        {
            if (CalcLevenshteinDistance(key1, key2) <= 3)
                return true;
            var expanded1 = ParseKeyAndGetDetails(key1);
            var expanded2 = ParseKeyAndGetDetails(key2);

            return AreNamesEquivalent(expanded1.player1, expanded2.player1) && AreNamesEquivalent(expanded1.player2, expanded2.player2);
        }

        static bool AreNamesEquivalent(dynamic name1, dynamic name2)
        {
            return (name1.firstName == name2.firstName || name1.firstName.StartsWith(name2.firstName) || name2.firstName.StartsWith(name1.firstName))
               && (name1.lastName == name2.lastName || name1.lastName.StartsWith(name2.lastName) || name2.lastName.StartsWith(name1.lastName));
        }

        /// <summary>
        /// Gets the similarity of two strings
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int CalcLevenshteinDistance(string a, string b)
        {
            if (String.IsNullOrEmpty(a) || String.IsNullOrEmpty(b)) return 0;

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min
                        (
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost
                        );
                }
            return distances[lengthA, lengthB];
        }
    }

    public static class MyExtensions
    {
        public static string GetKey(this MatchDetails match)
        {
            return match.HomePlayer + "|" + match.AwayPlayer;
        }

    }


}
