using Helpers;
using HtmlAgilityPack;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebsiteParsers
{
    public class WilliamHillParser : IWebsiteParser
    {
        private readonly IDictionary<string, string> _websiteLocations = new Dictionary<string, string>()
        {
            {"tennis", "http://sports.williamhill.com/bet/en-gb/betting/y/17/mh/Tennis.html"}
        };

        public IEnumerable<MatchDetails> ParseResultsForSport(SportDetails sport)
        {
            if(!_websiteLocations.ContainsKey(sport.Name))
            {
                throw new ArgumentException(string.Format("The sport {0} is not supported", sport.Name));
            }

            string response = WebHelpers.GetContentOfUrl(_websiteLocations[sport.Name]);

            var document = new HtmlDocument();
            document.LoadHtml(response);

            List<MatchDetails> matches = new List<MatchDetails>();
            var rows = document.DocumentNode.Descendants("tr").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("rowOdd"));
            foreach(var row in rows)
            {
                var match = GetMatchDetailFromTableRow(row);
                if(match != null)
                {
                    matches.Add(match);
                }
            }
            return matches;
        }

        private MatchDetails GetMatchDetailFromTableRow(HtmlNode row)
        {
            var columns = row.ChildNodes.Where(node => node.Name == "td").ToList();
            if (columns.Count != 10)
                return null;

            var homeOddsText = HtmlEntity.DeEntitize(columns[3].InnerText);
            var versusText = HtmlEntity.DeEntitize(columns[4].InnerText);
            var awayOddsText = HtmlEntity.DeEntitize(columns[5].InnerText);
            var splitted = versusText.Split(new string[] { " v " }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            MatchDetails match = new MatchDetails()
            {
                HomeOdds = GetDecimalFromBettingFraction(homeOddsText) + 1,
                AwayOdds = GetDecimalFromBettingFraction(awayOddsText) + 1,
                HomePlayer = splitted[0],
                AwayPlayer = splitted[1]
            };

            return match;
        }

        private decimal GetDecimalFromBettingFraction(string fraction)
        {
            if (fraction.ToLower().Trim() == "evs")
                return 1;
            string[] numbers = fraction.Split('/');
            return decimal.Parse(numbers[0]) / decimal.Parse(numbers[1]);
        }
    }
}
