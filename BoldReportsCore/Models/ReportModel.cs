using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BoldReportsCore.Models
{
    public class ReportModel
    {
        public string RootPath { get; set; }

        public ReportModel(string rootPath)
        {
            this.RootPath = rootPath;
        }

        public List<SampleList> GetReports()
        {
            List<SampleList> items = new List<SampleList>();
            DirectoryInfo directory = new DirectoryInfo(this.RootPath);
            var files = directory.GetFiles().ToList<FileInfo>().OrderBy(file => file.LastWriteTime).Reverse();
            List<string> reportList = new List<string>();

            foreach (var file in files)
            {
                SampleList item = new SampleList();
                item.Name = Path.GetFileNameWithoutExtension(file.Name);
                items.Add(item);
            }

            return items;
        }

        public string GetWebAPIToken()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    var content = new FormUrlEncodedContent(new[]
                   {
                    new KeyValuePair<string, string>("username", "guest@boldreports.com"),
                    new KeyValuePair<string, string>("apiKey", "uV5i0dZvFGcpSB7o7aL8kvm8DKqQhrA"),
                });
                    var result = client.PostAsync("https://customerdemo.boldreports.com/corewebapi/api/get-apikey", content).Result;
                    string resultContent = result.Content.ReadAsStringAsync().Result;

                    return JsonConvert.DeserializeObject<ApiToken>(resultContent).AccessToken;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    public class SampleList
    {
        public string Name { get; set; }

        public string Path { get; set; }
    }

    public class ApiToken
    {
        public string AccessToken { get; set; }

        public string UserName { get; set; }

        public string ExpiresIn { get; set; }

        public string TokenType { get; set; }
    }
}
