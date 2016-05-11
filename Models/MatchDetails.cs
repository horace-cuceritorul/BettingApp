using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    /// <summary>
    /// Class to represent the details of a match
    /// </summary>
    public class MatchDetails
    {
        /// <summary>
        /// The name of the home team
        /// </summary>
        public string HomePlayer { get; set; }

        /// <summary>
        /// The name of the away team
        /// </summary>
        public string AwayPlayer { get; set; }

        /// <summary>
        /// The odds when the match ends up in a draw
        /// </summary>
        public decimal? DrawOdds { get; set; }

        /// <summary>
        /// The success rate for home team
        /// </summary>
        public decimal HomeOdds { get; set; }

        /// <summary>
        /// The success rate for away team
        /// </summary>
        public decimal AwayOdds { get; set; }
    }
}
