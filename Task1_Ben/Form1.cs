using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Runtime.Caching;

namespace Task1_Ben
{
    public partial class Form1 : Form
    {

        public CachingProviderBase cache = new CachingProviderBase();
        public Form1()
        {
            InitializeComponent();
        }

        public class Argument
        {
            public List<Variable> variables { get; set; }
        }

        public class ContentScope
        {
            public string varName { get; set; }
            public Range range { get; set; }
        }

        public class LangScope
        {
            public string varName { get; set; }
            public Range range { get; set; }
        }

        public class Range
        {
            public string dataTypeName { get; set; }
            public string rangeTypeName { get; set; }
            public dynamic values { get; set; }
        }

        public class Root
        {
            public ContentScope contentScope { get; set; }
            public LangScope langScope { get; set; }
            public TimeScope timeScope { get; set; }
            public Argument argument { get; set; }
        }

        public class TimeScope
        {
            public string varName { get; set; }
            public Range range { get; set; }
        }

        public class Values
        {
            public string name { get; set; }
            public string min { get; set; }
            public string max { get; set; }
        }

        public class Variable
        {
            public string varName { get; set; }
            public Range range { get; set; }
        }

        public class SessionPayload
        {
            public string uuid { get; set; }
            public string login { get; set; }
            public string host { get; set; }
            public string openedAt { get; set; }
            public string expireAt { get; set; }
            public int duration { get; set; }
            public string closedAt { get; set; }
        }

        public class Payload
        {
            public string uuid { get; set; }
            public string onlineDocCode { get; set; }
            public string onlineDocLangCode { get; set; }
            public int onlineDocVerNo { get; set; }
            public string topicCode { get; set; }
            public int topicVerNo { get; set; }
            public string readerCountryCode { get; set; }
            public string readerLangCode { get; set; }
            public string readerOsCode { get; set; }
            public string readerBrowserCode { get; set; }
            public string acceptedAt { get; set; }
            public string messageTypeCode { get; set; }
            public string messageText { get; set; }
        }

        public class UuidJson
        {
            public int ver { get; set; }
            public int statusCode { get; set; }
            public string payloadTypeName { get; set; }
            public SessionPayload payload { get; set; }
        }

        public class MessagesJson {
            public int ver { get; set; }
            public int statusCode { get; set; }
            public string payloadTypeName { get; set; }
            public Payload[] payload { get; set; }
        }

        public HttpWebRequest BuildRequest(string baseUrl, string parameters, string method)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(baseUrl + parameters);
            req.Credentials = CredentialCache.DefaultCredentials;
            req.Method = method;
            req.Accept = "application/json";
            return req;
        }

        public async Task<T> FetchJsonAsync<T>(HttpWebRequest req)
        {
            //await Task.Delay(1000);
            var res = (HttpWebResponse)await req.GetResponseAsync();

            using (var sr = new StreamReader(res.GetResponseStream()))
            {
                var json = await sr.ReadToEndAsync();
                var result = JsonConvert.DeserializeObject<T>(json);
                res.Close();
                sr.Close();
                return result;
            }
        }

        public async Task<string> GetUuidAsync(string user, string password, string baseUrl)
        {
            //await Task.Delay(1000);
            string parameters = $"login?user={user}&password={password}";
            HttpWebRequest req = BuildRequest(baseUrl, parameters, "POST");

            UuidJson result = await FetchJsonAsync<UuidJson>(req);
            string uuid = result.payload.uuid;
            return uuid;
        }

        public string ConvertDateToString(DateTime date)
        {
            return $"{date.Year}-{date.Month}-{date.Day}";
        }

        public string BuildCacheKey(string topic, string startTime, string endTime, string langs)
        {
            return $"[{topic}][{startTime}][{endTime}][{langs}]";
        }

        public async Task<ResponseClass> MakeTopicSummariesRequest(DateTime startTime, DateTime endTime, string topic, bool includeAllLangs, List<string> langList)
        {
        string baseUrl = "http://89.108.102.59/fserv/v1/";
        string parameters = "reports/topic-summaries";
        HttpWebRequest webRequest = BuildRequest(baseUrl, parameters, "POST");

        string user = "ditatoo", password = "haveYouReadUlysses?";
        var uuid = await GetUuidAsync(user, password, baseUrl);
        webRequest.Headers.Add("Cookie", uuid);


        var variable = BuildObjectSerializer();
        List<Variable> varList = new List<Variable>() { variable };


        //string topic = "activating_biotech_psychos";
        List<string> topicList = new List<string>() { topic };
        //List<string> langList = new List<string> { "en", "de", "jp" };
        string startDate = ConvertDateToString(startTime), endDate = ConvertDateToString(endTime);
        var root = BuildRootObject(includeAllLangs, langList, topicList, startDate, endDate, varList);

        string json = JsonConvert.SerializeObject(root);

        byte[] byteArray = Encoding.UTF8.GetBytes(json);

        var reqStream = webRequest.GetRequestStream();
        reqStream.Write(byteArray, 0, byteArray.Length);

            var response = await webRequest.GetResponseAsync();

        var respStream = response.GetResponseStream();

        var reader = new StreamReader(respStream);
        string data = reader.ReadToEnd();

        // Deserialize the Json into a ResponseClass

        var responseClass = new ResponseClass(data);
        return responseClass;
        }

        public enum Indicators
        {
            COUNTRY,
            OS,
            BROWSER,
            LANG
        }

        public enum TimeSpans
        {
            MONTH, DAY, YEAR
        }

        public void FillDictionary(List<ResponseClass.Indicator> values, Dictionary<string, int> dict)
        {
            foreach (var v in values)
            {
                dict.Add(v.code, v.count);
            }
        }


        private int GetDaysInMonth(int month)
        {
            // jan mar may july aug oct dec 31
            // feb 28
            // april june sept nov 30

            int[] DAYS_31 = { 1, 3, 5, 7, 8, 10, 12 };
            int[] DAYS_30 = { 4, 6, 9, 11 };
            int number_of_days = 0;
            if (DAYS_30.Contains(month))
            {
                number_of_days = 30;
            }
            else if (DAYS_31.Contains(month))
            {
                number_of_days = 31;
            }
            else if (month == 2)
            {
                number_of_days = 28;
            }

            return number_of_days;

        }

        private DateTime CalculateMonthlyEndDate(DateTime startDate, int frequency)
        {
            int startMonth = startDate.Month;
            int num_days = 0;
            for (int i = 0; i < frequency; ++i)
            {
                num_days += GetDaysInMonth(startMonth);
                startMonth++;
            }
            return startDate.Add(new TimeSpan(num_days, 0, 0, 0));
        }


        // makes a request ***frequency months long starting from ***startTime
        // uses params topic, includeAllLangs, langList for the Request
        // this overload includes all local langs in the request
        public async Task<ResponseClass> MakeMonthlyRequestAsync(string topic, DateTime startDate, int frequency)
        {
            DateTime endDate = CalculateMonthlyEndDate(startDate, frequency);

            string key = BuildCacheKey(topic, ConvertDateToString(startDate), ConvertDateToString(endDate), "any");
            ResponseClass response;
            if (cache.GetItem(key) == null)
            {
                response = await MakeTopicSummariesRequest(startDate, endDate, topic, true, null);
                cache.AddItem(key, response);
            }
            else
            {
                response = cache.GetItem(key);
            }
            return response;
        }

        // makes a request ***frequency months long starting from ***startTime
        // uses params topic, includeAllLangs, langList for the Request
        // this overload includes a specified list of local langs in the request
        public async Task<ResponseClass> MakeMonthlyRequestAsync(string topic, DateTime startDate, int frequency, List<string> langList)
        {
            DateTime endDate = CalculateMonthlyEndDate(startDate, frequency);

            string key = BuildCacheKey(topic, ConvertDateToString(startDate), ConvertDateToString(endDate), String.Join(", ", langList.ToArray()));
            ResponseClass response;
            if (cache.GetItem(key) == null)
            {
                response = await MakeTopicSummariesRequest(startDate, endDate, topic, false, langList);
                cache.AddItem(key, response);
            }
            else
            {
                response = cache.GetItem(key);
            }
            return response;
        }

        // makes a request ***frequency days long starting from ***startTime
        // uses params topic, includeAllLangs, langList for the Request
        // this overload includes all local langs in the request
        public async Task<ResponseClass> MakeDailyRequestAsync(string topic, DateTime startDate, int frequency)
        {
            DateTime endDate = startDate.Add(new TimeSpan(frequency, 0, 0, 0));

            string key = BuildCacheKey(topic, ConvertDateToString(startDate), ConvertDateToString(endDate), "any");
            ResponseClass response;
            if (cache.GetItem(key) == null)
            {
                response = await MakeTopicSummariesRequest(startDate, endDate, topic, true, null);
                cache.AddItem(key, response);
            }
            else
            {
                //label1.Text = "cache works";
                response = cache.GetItem(key);
            }
            return response;
        }

        // makes a request ***frequency days long starting from ***startTime
        // uses params topic, includeAllLangs, langList for the Request
        // this overload includes a specified list of local langs in the request
        public async Task<ResponseClass> MakeDailyRequestAsync(string topic, DateTime startDate, int frequency, List<string> langList)
        {
            DateTime endDate = startDate.Add(new TimeSpan(frequency, 0, 0, 0));

            string key = BuildCacheKey(topic, ConvertDateToString(startDate), ConvertDateToString(endDate), String.Join(", ", langList.ToArray()));
            ResponseClass response;
            if (cache.GetItem(key) == null)
            {
                response = await MakeTopicSummariesRequest(startDate, endDate, topic, false, langList);
                cache.AddItem(key, response);
            }
            else
            {
                response = cache.GetItem(key);
            }
            return response;
        }

        private TimeSpan CalculateIntervalLength(DateTime startDate, int frequency)
        {
            int startMonth = startDate.Month;
            int num_days = 0;
            for (int i = 0; i < frequency; ++i)
            {
                num_days += GetDaysInMonth(startMonth);
                startMonth++;
            }
            return new TimeSpan(num_days, 0, 0, 0);
        }

        public async Task<List<Dictionary<string, int>>> FilterResponseAsync(string startTime, string endTime, TimeSpans interval, Indicators indicator, string topic, int frequency)
        {
            DateTime startDate = Convert.ToDateTime(startTime);
            DateTime endDate = Convert.ToDateTime(endTime);

            List<Dictionary<string, int>> dictionaryList = new List<Dictionary<string, int>>();

            while (startDate < endDate)
            {
                TimeSpan newInterval = (interval == TimeSpans.MONTH) ? CalculateIntervalLength(startDate, frequency) : new TimeSpan(frequency, 0, 0, 0);
                try
                {

                    ResponseClass response;
                    if (interval == TimeSpans.MONTH)
                    {
                        response = await MakeMonthlyRequestAsync(topic, startDate, frequency);
                    }
                    else if (interval == TimeSpans.DAY)
                    {
                        response = await MakeDailyRequestAsync(topic, startDate, frequency);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid \"interval\", {interval}, not supported");
                        return null;
                    }



                    Dictionary<string, int> dict = new Dictionary<string, int>();

                    switch (indicator)
                    {
                        case Indicators.COUNTRY:
                            {
                                FillDictionary(response.rootObject.payload[0].values.countries, dict);
                                break;
                            }
                        case Indicators.OS:
                            {
                                FillDictionary(response.rootObject.payload[0].values.oss, dict);
                                break;
                            }
                        case Indicators.BROWSER:
                            {
                                FillDictionary(response.rootObject.payload[0].values.browsers, dict);
                                break;
                            }
                        case Indicators.LANG:
                            {
                                FillDictionary(response.rootObject.payload[0].values.langs, dict);
                                break;
                            }
                        default: break;
                    }
                    label1.Text += $"\nstart={startDate}, end={startDate.Add(newInterval)}\n";

                    dictionaryList.Add(dict);
                    startDate = startDate.Add(newInterval);
                }
                catch
                {
                    Console.WriteLine($"\nNo data for interval:\t start ={ startDate}, end ={ startDate.Add(newInterval)}\n");
                    startDate = startDate.Add(newInterval);
                }
            }
            return dictionaryList;
        }


        public async Task<List<Dictionary<string, int>>> FilterResponseAsync(string startTime, string endTime, TimeSpans interval, Indicators indicator, string topic, int frequency, List<string> langList)
        {
            DateTime startDate = Convert.ToDateTime(startTime);
            DateTime endDate = Convert.ToDateTime(endTime);

            List<Dictionary<string, int>> dictionaryList = new List<Dictionary<string, int>>();

            while (startDate < endDate)
            {
                TimeSpan newInterval = (interval == TimeSpans.MONTH) ? CalculateIntervalLength(startDate, frequency) : new TimeSpan(frequency, 0, 0, 0);
                try
                {

                    ResponseClass response;
                    if (interval == TimeSpans.MONTH)
                    {
                        response = await MakeMonthlyRequestAsync(topic, startDate, frequency, langList);
                    }
                    else if (interval == TimeSpans.DAY)
                    {
                        response = await MakeDailyRequestAsync(topic, startDate, frequency, langList);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid \"interval\", {interval}, not supported");
                        return null;
                    }



                    Dictionary<string, int> dict = new Dictionary<string, int>();

                    switch (indicator)
                    {
                        case Indicators.COUNTRY:
                            {
                                FillDictionary(response.rootObject.payload[0].values.countries, dict);
                                break;
                            }
                        case Indicators.OS:
                            {
                                FillDictionary(response.rootObject.payload[0].values.oss, dict);
                                break;
                            }
                        case Indicators.BROWSER:
                            {
                                FillDictionary(response.rootObject.payload[0].values.browsers, dict);
                                break;
                            }
                        case Indicators.LANG:
                            {
                                FillDictionary(response.rootObject.payload[0].values.langs, dict);
                                break;
                            }
                        default: break;
                    }
                    label1.Text += $"\nstart={startDate}, end={startDate.Add(newInterval)}\n";

                    dictionaryList.Add(dict);
                    foreach (var d in dict)
                    {
                        label1.Text += $"{d.Key}={d.Value},";
                    }
                    label1.Text += "\n";
                    startDate = startDate.Add(newInterval);
                }
                catch
                {
                    Console.WriteLine($"\nNo data for interval:\t start ={ startDate}, end ={ startDate.Add(newInterval)}\n");
                    label1.Text += $"request failed";
                    startDate = startDate.Add(newInterval);
                }
            }
            return dictionaryList;
        }

        private async Task<ResponseClass> MakeCacheRequest(DateTime startTime, DateTime endTime, string topic)
        {


            string key = BuildCacheKey(topic, ConvertDateToString(startTime), ConvertDateToString(endTime), "any");
            ResponseClass response;
            if (cache.GetItem(key) == null)
            {
                response = await MakeTopicSummariesRequest(startTime, endTime, topic, true, null);
                cache.AddItem(key, response);
            }
            else
            {
                response = cache.GetItem(key);
            }
            return response;


            //assuming -1.0 is considered invalid to represent an error, could always change return type to string
            //return response;
        }


        private async Task<ResponseClass> MakeCacheRequest(DateTime startTime, DateTime endTime, string topic, List<string> langList)
        {


            string key = BuildCacheKey(topic, ConvertDateToString(startTime), ConvertDateToString(endTime), String.Join(", ", langList.ToArray()));
            ResponseClass response;
            if (cache.GetItem(key) == null)
            {
                response = await MakeTopicSummariesRequest(startTime, endTime, topic, false, langList);
                cache.AddItem(key, response);
            }
            else
            {
                response = cache.GetItem(key);
            }
            return response;


            //assuming -1.0 is considered invalid to represent an error, could always change return type to string
            //return response;
        }

        public async Task<double> CalculateBadness(DateTime startTime, DateTime endTime, string topic)
        {
            ResponseClass response = await MakeCacheRequest(startTime, endTime, topic);
            return response.rootObject.payload[0].values.badness;
        }

        public async Task<double> CalculateBadness(DateTime startTime, DateTime endTime, string topic, List<string> langList)
        {
            ResponseClass response = await MakeCacheRequest(startTime, endTime, topic, langList);
            return response.rootObject.payload[0].values.badness;
        }



        private async void button1_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            Indicators indicator = Indicators.COUNTRY;

            if(textBox1.Text == "c")
            {
                indicator = Indicators.COUNTRY;
            }else if(textBox1.Text == "o")
            {
                indicator = Indicators.OS;
            }
            else if (textBox1.Text == "b")
            {
                indicator = Indicators.BROWSER;
            }
            else if (textBox1.Text == "l")
            {
                indicator = Indicators.LANG;
            }

            TimeSpans interval = TimeSpans.MONTH;
            
            string startTime = "2022-01-01", endTime = "2022-05-30";
            
            string topic = "activating_biotech_psychos";
            int frequency = 2;
            List<string> langList = new List<string> { "en", "de", "jp" };

            double bad = await CalculateBadness(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime), topic, langList);
            label1.Text = bad.ToString();

            //List<Dictionary<string, int>> dictionaryList = await FilterResponseAsync(startTime, endTime, interval, indicator, topic, frequency);

            //while (startDate < endDate)
            //{
            //    try
            //    {
            //        var response = await MakeTopicSummariesRequest(startDate, startDate.Add(interval), "activating_biotech_psychos");
            //        Dictionary<string, int> dict = new Dictionary<string, int>();

            //        switch (indicator)
            //        {
            //            case Indicators.COUNTRY:
            //                {
            //                    FillDictionary(response.rootObject.payload[0].values.countries, dict);
            //                    break;
            //                }
            //            case Indicators.OS:
            //                {
            //                    FillDictionary(response.rootObject.payload[0].values.oss, dict);
            //                    break;
            //                }
            //            case Indicators.BROWSER:
            //                {
            //                    FillDictionary(response.rootObject.payload[0].values.browsers, dict);
            //                    break;
            //                }
            //            case Indicators.LANG:
            //                {
            //                    FillDictionary(response.rootObject.payload[0].values.langs, dict);
            //                    break;
            //                }
            //            default: break;
            //        }
            //        label1.Text += $"\nstart={startDate}, end={startDate.Add(interval)}\n";
            //        dictionaryList.Add(dict);
            //        startDate = startDate.Add(interval);
            //    }
            //    catch
            //    {
            //        label1.Text += $"\nNo data for interval:\t start ={ startDate}, end ={ startDate.Add(interval)}\n";
            //        startDate = startDate.Add(interval);
            //    }



            //}



            //foreach(var v in dictionaryList)
            //{
            //    label1.Text += "\n";
            //    foreach (var d in v)
            //    {
            //        label1.Text += $"{d.Key}={d.Value},";
            //    }
            //    label1.Text += "\n";
            //}





        }

        private Variable BuildObjectSerializer()
        {
            return new Variable()
            {
                varName = "topicCode",
                range = new Range()
                {
                    dataTypeName = "string",
                    rangeTypeName = "named",
                    values = new Values() { name = "any" }
                },
            };


        }

        private Root BuildRootObject(bool named, object langs, List<string> topics, string startDate, string endDate, List<Variable> varList)
        {
            return new Root()
            {
                contentScope = new ContentScope()
                {
                    varName = "topicCode",
                    range = new Range()
                    {
                        dataTypeName = "string",
                        rangeTypeName = "list",
                        values = topics,
                    },
                },
                langScope = new LangScope()
                {
                    varName = "onlineDocLangCode",
                    range = new Range()
                    {
                        dataTypeName = "string",
                        rangeTypeName = named ? "named" : "list",
                        values = named ? new Values() { name = "any" } : langs
                    },
                },
                timeScope = new TimeScope()
                {
                    varName = "acceptedAt",
                    range = new Range()
                    {
                        dataTypeName = "timestamp",
                        rangeTypeName = "segment",
                        values = new Values()
                        {
                            min = startDate,
                            max = endDate,
                        }
                    },
                },
                argument = new Argument()
                {
                    variables = varList,
                },
            };
        }


    }
}
