using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DomainChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            // output instructions
            Console.WriteLine("Enter the full path to your text file of domains. Example: C:\\Users\\bob\\Desktop\\domains.txt");
            Console.WriteLine("One domain per line, like this:");
            Console.WriteLine("domain.com");
            Console.WriteLine("mysite.com");

            //process input
            string userInput = Console.ReadLine();
            string[] listOfDomains = File.ReadAllLines(userInput);
            Console.WriteLine(listOfDomains.Length);

            //create list of objects
            List<Domain> listofScannedDomains = new List<Domain>();

            Parallel.ForEach(listOfDomains, new ParallelOptions { MaxDegreeOfParallelism = 12 }, domain =>
            {

                // try with www and without, as many servers don't redirect
                try
                {
                    try
                    {
                        var startUrl = "http://" + domain;
                        listofScannedDomains.Add(CheckUrl(startUrl, domain));
                    }
                    catch (Exception)
                    {
                        var startUrl = "http://www." + domain;
                        listofScannedDomains.Add(CheckUrl(startUrl, domain));
                    }
                }

                catch (Exception e)
                {
                    // for domains that errored out
                    listofScannedDomains.Add(new Domain()
                    {
                        DomainUrl = domain,
                        ResponseUrl = "Error",
                        Status = "Error",
                        HostingLocation = "Error",
                        Details = e.Message.Replace(",", "")
            });
                }
            });

            // create csv
            var csv = new StringBuilder();
            var firstLine = string.Format("Domain,Status,Hosting Location,Response URL,Details");
            csv.AppendLine(firstLine);
            foreach (var item in listofScannedDomains)
            {
                var nextLine = string.Format("{0},{1},{2},{3},{4}", item.DomainUrl, item.Status, item.HostingLocation, item.ResponseUrl, item.Details);
                csv.AppendLine(nextLine);
            }
            File.WriteAllText("output.csv", csv.ToString());
            Console.WriteLine("Done! Please check the csv file in the directory you ran this program in.");

            Console.ReadLine();
        }
        public static Domain CheckUrl(string startUrl, string domain)
        {
            HttpWebResponse response = null;

                HttpWebRequest requestUrl = (HttpWebRequest)HttpWebRequest.Create(startUrl);
                requestUrl.Method = "GET";
                requestUrl.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
                requestUrl.AllowAutoRedirect = true;
                requestUrl.Timeout = 5000;
                response = (HttpWebResponse)requestUrl.GetResponse();

                StreamReader sr = new StreamReader(response.GetResponseStream());


            string responseUrl = response.ResponseUri.ToString();
            string responseUrlLocation = "Unknown";
            response.Close();
            Uri crawledUri = new Uri(startUrl);
            Uri returnedUri = new Uri(responseUrl);
            if (crawledUri.Host == returnedUri.Host || "www." + crawledUri.Host == returnedUri.Host)
            {
                responseUrlLocation = "Same Site";
            }
            else
            {
                responseUrlLocation = "Redirects To Other Site";
            }
            var domainData = new Domain()
            {
                DomainUrl = domain,
                ResponseUrl = responseUrl,
                HostingLocation = responseUrlLocation
            };
            return domainData;
        }
    }
    class Domain
    {
        public string DomainUrl { get; set; }
        public string ResponseUrl { get; set; }
        public string Status { get; set; }
        public string HostingLocation { get; set; }
        public string Details { get; set; }
    }
}
