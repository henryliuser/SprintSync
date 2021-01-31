using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;



namespace sonic
{
    class Sonic
    {
        static async Task Main(string[] args)
        {

            const String BROWNEYESPATH = "C:\\Users\\sona\\source\\repos\\sonic\\sonic_data.txt";

            // working url
            // "https://api.sonicAPI.com/analyze/tempo?access_id=640e2b51-3650-430b-8eb5-2d01d82e4885&input_file=http%3A%2F%2Fwww.sonicAPI.com%2Fmusic%2Fbrown_eyes_by_ueberschall.mp3&format=json";

            // FROM API
            Song s1 = await Song.CreateAsync("http://www.sonicAPI.com/music/brown_eyes_by_ueberschall.mp3");

            // FROM LOCAL
            //Song s1 = Song.CreateSongFromLocal(BROWNEYESPATH);

            Console.WriteLine("Overall Tempo: {0}", s1.GetOverallTempo() + "\n");
            Console.WriteLine("Sections:");
            //foreach (KeyValuePair<double, double> entry in s1.GetSectionDict())
            //{
            //    Console.WriteLine("(Time) {0} -- (BPM) {1}", entry.Key, entry.Value);
            //}
            Console.WriteLine("\n");
            Console.WriteLine(String.Join("\n", s1.GetClickMarks()));


        }
        public class Song
        {
            private static String SONICAPIKEY = "640e2b51-3650-430b-8eb5-2d01d82e4885";
            private static String BASEURL = "https://api.sonicAPI.com/";


            private double overallTempo;
            private List<dynamic> clickMarks;
            private List<int[]> sectionDict;

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            public static Song CreateSongFromLocal(String file)
            {
                Song x = new Song();
                x.InitializeLocal(file);
                return x;
            }

            private void InitializeLocal(String file)
            {
                var jsonDict = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(File.ReadAllText(file));
                var tempoDict = jsonDict["auftakt_result"];

                overallTempo = Convert.ToDouble(tempoDict["overall_tempo_straight"]);
                clickMarks = tempoDict["click_marks"].ToObject<List<dynamic>>();
                sectionDict = InitializeSections(clickMarks);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            public static async Task<Song> CreateAsync(String file)
            {
                Song x = new Song();
                await x.InitializeAsyncAPI(file);
                return x;
            }



            private async Task InitializeAsyncAPI(String file)
            {
                String searchUrl = BuildSearchUrl(SONICAPIKEY, "analyze/tempo", file);
                Dictionary<string, dynamic> data = await DownloadData(file, searchUrl);
                var tempoDict = data["auftakt_result"];

                overallTempo = Convert.ToDouble(tempoDict["overall_tempo_straight"]);
                clickMarks = tempoDict["click_marks"].ToObject<List<dynamic>>();
                sectionDict = InitializeSections(clickMarks);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            public Song() { }

            private static List<int[]> InitializeSections(List<dynamic> clickList)
            {
                List<int[]> L = new List<int[]>();
                int prevBpm = 0;

                // bpm difference of 6 is considered significant enough to demarcate a new section
                int idx = 1;
                bool first = true;
                int lastStartOfSection = 0;
                foreach (var click in clickList)
                {
                    int bpm = Convert.ToInt32(click["bpm"]);
                    int time = Convert.ToInt32(click["time"]);
                    if (Math.Abs(bpm - prevBpm) >= 6)
                    {
                        if (first) { L.Add(new int[] { 0, bpm }); time = 0; first = false; }
                        else
                        {
                            L[idx++][0] = time - lastStartOfSection;
                            L.Add(new int[] { 0, bpm });
                            lastStartOfSection = time;
                        }
                        
                    }

                    prevBpm = bpm;
                    if (Convert.ToInt32(click["index"]) == clickList.Count-1)
                    {
                        L[idx-1][0] = time - lastStartOfSection;
                    }
                }
                return L;
            }

            public double GetOverallTempo()
            {
                return overallTempo;
            }

            public List<dynamic> GetClickMarks()
            {
                return clickMarks;
            }

            public List<int[]> GetSectionDict()
            {
                return sectionDict;
            }
            // "https://api.sonicAPI.com/analyze/tempo?access_id=640e2b51-3650-430b-8eb5-2d01d82e4885&input_file=%2fstorage%2femulated%2f0%2fAndroid%2fdata%2fcom.idk.sprintsync.sprintsync%2fcache%2f2203693cc04e0be7f4f024d5f9499e13%2ffe8792bb7dc9401a83b790d0e9b196d6%2frepresen…"
            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            private static String BuildSearchUrl(String key, String task, String localPath)
            {
                NameValueCollection queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
                queryParams.Add("access_id", key);
                queryParams.Add("input_file", localPath);
                queryParams.Add("format", "json");

                String queryString = queryParams.ToString();
                String url = String.Format("{0}?{1}", task, queryString);

                //Console.WriteLine(url);

                return BASEURL + url;
            }
            private static async Task<Dictionary<string, dynamic>> DownloadData(String file, String url)
            {
                dynamic response;


                string urlb = string.Format("https://api.sonicapi.com/file/upload?access_id={0}&format=json", SONICAPIKEY);
                var stream = new FileStream(file, FileMode.Open);
                string name = Path.GetFileName(file);
                name = "hello.mp3";
                var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                streamContent.Headers.ContentDisposition.Name = "\"file\"";
                streamContent.Headers.ContentDisposition.FileName = "\"" + name + "\"";
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var content = new MultipartFormDataContent { streamContent };
                HttpResponseMessage message = await client.PostAsync(urlb, content);
                
                string s = await message.Content.ReadAsStringAsync();
                //using (var myStream = File.OpenRead(file))
                //{
                //var client = new HttpClient();
                ////client.BaseAddress = new Uri(BASEURL);
                ////var mp3 = new MultipartFormDataContent();
                ////mp3.Add(new StreamContent(myStream), "file");

                //var form = new MultipartFormDataContent();
                //ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(file));
                //fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                //form.Add(fileContent, "file", Path.GetFileName(file));
                ////response = await client.PostAsync(url, )
                
                var response_dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(s);
                string fileid = response_dict["file"]["file_id"].ToString().Replace("\"", "");

                urlb = string.Format("https://api.sonicapi.com/analyze/tempo?access_id={0}&format=json&input_file={1}", SONICAPIKEY, fileid);
                client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                response = await client.GetStringAsync(urlb);
                return JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response);


            }


        }

    }
}
