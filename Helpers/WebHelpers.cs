using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>
    /// A class with web-related helpers
    /// </summary>
    public static class WebHelpers
    {
        /// <summary>
        /// Gets the raw html response from specified url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetContentOfUrl(string url)
        {
            var request = HttpWebRequest.Create(url);
            var response = request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}
