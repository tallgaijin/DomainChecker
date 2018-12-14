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
                        var startUrl = "https://" + domain;
                        listofScannedDomains.Add(CheckUrl(startUrl, domain));
                    }
                    catch (Exception)
                    {
                        var startUrl = "https://www." + domain;
                        listofScannedDomains.Add(CheckUrl(startUrl, domain));
                    }
                }
                catch (Exception)
                {
                    // for domains that errored out
                    listofScannedDomains.Add(new Domain()
                    {
                        DomainUrl = domain,
                        ResponseUrl = "Error",
                        Status = "Error",
                        Details = "Unknown"
                    });
                }
            });

            // create csv
            var csv = new StringBuilder();
            var firstLine = string.Format("Domain,Status,Response URL,Details");
            csv.AppendLine(firstLine);
            foreach (var item in listofScannedDomains)
            {
                var nextLine = string.Format("{0},{1},{2},{3}", item.DomainUrl, item.Status, item.ResponseUrl, item.Details);
                csv.AppendLine(nextLine);
            }
            File.WriteAllText("output.csv", csv.ToString());
            Console.WriteLine("Done! Please check the csv file in the directory you ran this program in.");

            Console.ReadLine();
        }
        public static Domain CheckUrl(string startUrl, string domain)
        {
            HttpWebRequest request;
            string responseUrlStatus;
            request = (HttpWebRequest)WebRequest.Create(startUrl);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
            request.AllowAutoRedirect = true;
            request.Timeout = 5000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseUrl = response.ResponseUri.ToString();
            response.Close();
            Uri crawledUri = new Uri(startUrl);
            Uri returnedUri = new Uri(responseUrl);
            if (crawledUri.Host == returnedUri.Host)
            {
                responseUrlStatus = "OK";
            }
            else
            {
                responseUrlStatus = "Other";
            }
            var domainData = new Domain()
            {
                DomainUrl = domain,
                ResponseUrl = responseUrl,
                Status = responseUrlStatus
            };
            return domainData;
        }
    }
    class Domain
    {
        public string DomainUrl { get; set; }
        public string ResponseUrl { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
    }
}
