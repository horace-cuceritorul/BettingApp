using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebsiteParsers
{
    public interface IWebsiteParser
    {
        IEnumerable<MatchDetails> ParseResultsForSport(SportDetails sport);
    }
}
