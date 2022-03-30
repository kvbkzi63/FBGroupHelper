using Facebook;
using FBGroupHelper.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FBGroupHelper.Helper
{
    public class FBHelper
    {
        #region      Instance
 
        private FBHelper() { }

        public static FBHelper Instance { get { return new FBHelper(); } }

        #endregion 
        public void WriteLog(string Msg)
        {
            string AppLogFile = Application.StartupPath + @"\error.log";
            try
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(AppLogFile, true))
                {
                    outfile.WriteLine("Time:" + DateTime.Now.ToString() + " Message:" + Msg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// 取得社團全部貼文
        /// </summary>
        /// <param name="PAGE_TOKEN">粉專Token</param>
        /// <param name="GROUP_ID">社團ID</param>
        /// <returns></returns>
        public List<PostsModel> GetGroupPosts(string PAGE_TOKEN,string GROUP_ID)
        {
            FacebookClient fb = new FacebookClient(PAGE_TOKEN);
            try
            {

                JsonObject post1 = fb.Get<JsonObject>($"{GROUP_ID}/feed/"); 
                    var posts = post1["data"];
                    List<PostsModel> data = JsonConvert.DeserializeObject<List<PostsModel>>(posts.ToString()); 
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString()); 
                throw ex;
             }
        }
        /// <summary>
        /// 取得社團全部貼文
        /// </summary>
        /// <param name="PAGE_TOKEN">粉專Token</param>
        /// <param name="GROUP_ID">社團ID</param>
        /// <returns></returns>
        public List<PostsModel> GetGroupPostsByDate(string PAGE_TOKEN, string GROUP_ID, DateTime POST_SDATE, DateTime POST_EDATE)
        {
            FacebookClient fb = new FacebookClient(PAGE_TOKEN);
            try
            {
 
                Int32 unixSTimestamp = (Int32)(POST_SDATE.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Int32 unixETimestamp = (Int32)(POST_EDATE.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                JsonObject post1 = fb.Get<JsonObject>($"{GROUP_ID}/feed?since={unixSTimestamp}&until={unixETimestamp}&limit={200}");
                var posts = post1["data"];
                List<PostsModel> data = JsonConvert.DeserializeObject<List<PostsModel>>(posts.ToString()); 
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        } 
        public List<ParserModel> FromXML(string path)
        {
            XDocument xDoc = XDocument.Load(path);
            IEnumerable<ParserModel> table1 =
                (from s in xDoc.Descendants("ParserModel")
                 select new ParserModel()
                 {
                     message = s.Element("message") != null ? s.Element("message").Value : null,
                     id = Convert.ToInt32(s.Element("id").Value)
                 }).ToList();
            return table1.ToList() ; 
        }
        public List<CommentsModel> LeftComments(string path)
        {
            XDocument xDoc = XDocument.Load(path);
            IEnumerable<CommentsModel> table1 =
                (from s in xDoc.Descendants("CommentsModel")
                 select new CommentsModel()
                 {
                     message = s.Element("message") != null ? s.Element("message").Value : null,
                     post_id = s.Element("post_id") != null ? s.Element("post_id").Value : null,
                     reply_message = s.Element("reply_message") != null ? s.Element("reply_message").Value : null,
                     id = s.Element("id").Value
                 }).ToList();
            return table1.ToList();
        }
        public List<DynamicCharModel> DynamicChar(string path)
        {
            XDocument xDoc = XDocument.Load(path);
            IEnumerable<DynamicCharModel> table1 =
                (from s in xDoc.Descendants("DynamicCharModel")
                 select new DynamicCharModel()
                 {
                     start = s.Element("start") != null ? s.Element("start").Value : null,
                     end = s.Element("end") != null ? s.Element("end").Value : null 
                 }).ToList();
            return table1.ToList();
        }
        public string ToXML<T>(T obj)
        {
            using (StringWriter stringWriter = new StringWriter(new StringBuilder()))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                xmlSerializer.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }
        public enum xmlName
        {
            parserData,
            comments
        }
        public void SaveXml(string xml, xmlName fileName)
        {
             XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            xdoc.Save($"{fileName.ToString()}.xml");
        }
        /// <summary>
        /// 向指定URL發起請求(可用於遠端傳送資料)
        /// </summary>
        /// <param name="url"> 
        /// <returns></returns>
        public bool CheckAuthIsActived()
        {
            try
            {
                string key = "e36b6114bc61e4ef3d787a889a33f3491d9ef4982fd25503c58c01151ed37c20";
                string pw = "6052f43de8491216";
                string unixSTimestamp = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();

                Uri url = new Uri("https://demo.luckywave.com.tw/v1/service/check");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                AuthModel data = new AuthModel() { api_key = key, password = pw, time = unixSTimestamp };  
                 using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                { 
                    streamWriter.Write(JsonConvert.SerializeObject(data));
                } 
                string responseStr = "";
              
                //發出Request
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()  ))
                    {
                        responseStr = sr.ReadToEnd();

                        TokenModel result = JsonConvert.DeserializeObject<TokenModel>(responseStr);
                        return result.result;
                    } 
                } 
            }
            catch(Exception ex)  
            {
                return false;
            }
        }
    }
}
