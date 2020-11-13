using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string UserName = "r.savchenko";//rv.sav.ua@gmail.com
            string Password = "RSuds8444"; //token, not password:URskwZoyEIef7z9KVbED37DA
            string Url = "https://jira.uds.systems/";//https://sav-dev.atlassian.net/

            JiraConnection connection = new JiraConnection()
            {
                url = Url,
                username = UserName,
                apiToken = Password,
                projectKey = "BB"
            };

            string restUrl = String.Format("{0}rest/api/latest/issue/BB-1", connection.url);
            HttpWebResponse response = null;
            HttpWebRequest request = WebRequest.Create(restUrl) as HttpWebRequest;
            request.Method = "GET";
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Basic " + GetEncodedCredentials(connection.username, connection.apiToken));
          
            using (response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    string str = reader.ReadToEnd();
                    Regex keyReg = new Regex(@",""key"":"".*"",");
                    if (keyReg.Matches(str).Count > 0)
                    {
                        string issueKey = keyReg.Matches(str)[0].Value.Replace(",\"key\":\"", "")
                            .Replace("\",", "");
                        string issueLink = connection.url + "browse/" + issueKey;

                    }



                    //Console.WriteLine("The server returned '{0}'\n{1}", response.StatusCode, str);
                    //var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                    //var sData = jss.Deserialize<Dictionary<string,
                    //   string>>(str);
                    //string issueKey = sData["key"].ToString();
                    //showSuccess("Issue created sucessfully.");
                    //AddAttachments(issueKey);
                }
                else
                {
                    //showError("Error returned from Server:" + response.StatusCode + " Status Description : " + response.StatusDescription);
                }
            }
            request.Abort();


        }

        public static string GetEncodedCredentials(string UserName, string Password)
        {
            string mergedCredentials = String.Format("{0}:{1}", UserName, Password);
            byte[] byteCredentials = Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }

        public class JiraConnection
        {
            public string url { get; set; }
            public string username { get; set; }
            public string apiToken { get; set; }
            public string projectKey { get; set; }

        }
    }
}
