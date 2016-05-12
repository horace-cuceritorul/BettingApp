using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Helpers;
using HtmlAgilityPack;

namespace WebsiteParsers
{
    public class BetfairParser : IWebsiteParser
    {
        /// <summary>
        /// The locations of the websites for different sports
        /// </summary>
        private readonly IDictionary<string, string> _websiteLocations = new Dictionary<string, string>()
        {
            { "tennis", "https://www.betfair.com/sport/tennis" }
        };

        public IEnumerable<MatchDetails> ParseResultsForSport(SportDetails sport)
        {
            if (!_websiteLocations.ContainsKey(sport.Name))
            {
                throw new ArgumentException(String.Format("The sport {0} is not supported"), sport.Name);
            }

            var url = _websiteLocations[sport.Name];
            var response = WebHelpers.GetContentOfUrl(url);

            var document = new HtmlDocument();
            document.LoadHtml(response);

            var findclasses = document.DocumentNode.Descendants("ul").Where(d =>
                d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("event-list")
            );

            if (findclasses.Count() < 2)
            {
                throw new Exception("Betfair changed their layout!");
            }
            var listElement = findclasses.Last();

            List<MatchDetails> matches = new List<MatchDetails>();
            foreach (var element in listElement.ChildNodes.Where(node => node.Name == "li"))
            {
                var match = ExtractMatchDetailsFromListItem(element);
                if(match != null)
                {
                    matches.Add(match);
                }
            }

            return matches;
        }

        private MatchDetails ExtractMatchDetailsFromListItem(HtmlNode listItem)
        {
            MatchDetails matchDetails = new MatchDetails();

            //extracting the home team name
            var homeTeamElement = listItem.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("home-team-name")).FirstOrDefault();
            if (homeTeamElement == null)
            {
                return null;
            }
            var titleAttribute = homeTeamElement.Attributes["title"];
            matchDetails.HomePlayer = titleAttribute != null ? titleAttribute.Value : null;

            //extracting away team name
            var awayTeamElement = listItem.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("away-team-name")).FirstOrDefault();
            if (awayTeamElement == null)
            {
                return null;
            }
            titleAttribute = awayTeamElement.Attributes["title"];
            matchDetails.AwayPlayer = titleAttribute != null ? titleAttribute.Value : null;

            //extracting the bet values for players
            var uiRunnerPrice = listItem.Descendants("span").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("ui-runner-price")).ToList();
            if (uiRunnerPrice.Count != 2)
            {
                return null;
            }

            matchDetails.HomeOdds = GetDecimalFromBettingFraction(uiRunnerPrice[0].InnerText) + 1;
            matchDetails.AwayOdds = GetDecimalFromBettingFraction(uiRunnerPrice[1].InnerText) + 1;

            if (matchDetails.HomeOdds < 0 || matchDetails.AwayOdds < 0)
                return null;

            return matchDetails;
        }

        private decimal GetDecimalFromBettingFraction(string fraction)
        {
            if (fraction.ToLower().Trim() == "evs")
                return 1;
            string[] numbers = fraction.Split('/');
            if (numbers.Length != 2)
                return -100;
            return decimal.Parse(numbers[0]) / decimal.Parse(numbers[1]);
        }
    }
}
