using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Diagnostics;

namespace AYTHKeyGet
{
    class Program
    {
        /* **************************************
         * TODO:
         * Implement exception handlers
         * Verify files are there (swfextract, swf, png)
         * **************************************/
        static void Main(string[] args)
        {
            const string playerURL = "http://radiko.jp/player/swf/player_4.0.0.00.swf";
            const string auth1URL = "https://radiko.jp/v2/api/auth1_fms";
            const string auth2URL = "https://radiko.jp/v2/api/auth2_fms";
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); // application path
            string toolFile = appPath + "\\swfextract.exe"; // swfextract path
            string swfFile = System.Environment.GetEnvironmentVariable("TEMP") + "\\" + Process.GetCurrentProcess().Id.ToString() + "_player.swf";  // saved player file path at temp folder
            string pngFile = System.Environment.GetEnvironmentVariable("TEMP") + "\\" + Process.GetCurrentProcess().Id.ToString() + "_authkey.png"; // authkey png file path at temp folder

            /* **************************************
             * Check
             * **************************************/
            Console.WriteLine("toolFile: {0}", toolFile);
            if (!File.Exists(toolFile))
            {
                string errMsg = toolFile + " が見つかりません。AYTHKeyGet_ReadMe.txtを確認し、セットアップが正しくできているか確認して下さい。";
                throw new Exception(errMsg);
            }
            Console.WriteLine("swfFile: {0}", swfFile);
            Console.WriteLine("pngFile: {0}", pngFile);

            /* **************************************
             * Save player
             * **************************************/
            HttpWebRequest req =  (System.Net.HttpWebRequest) WebRequest.Create(playerURL);
            Console.WriteLine("Request to: {0}", req.RequestUri);
            HttpWebResponse res = (System.Net.HttpWebResponse) req.GetResponse();   // This may cause unhandled exception.

            Console.WriteLine("HTTP Status Code: {0}", res.StatusCode.ToString());
            if (res.StatusCode == HttpStatusCode.OK)
            {
                // save received swf file.
                Stream stream = res.GetResponseStream();
                FileStream fs1 = new FileStream(swfFile, FileMode.Create, FileAccess.Write);

                int b;
                while ((b = stream.ReadByte()) != -1)
                    fs1.WriteByte(Convert.ToByte(b));

                fs1.Close();
                stream.Close();
                Console.WriteLine("Finished saving swf.");
            }

            /* **************************************
             * Extract key using SWFTools
             * **************************************/
            // Verify file exists
            if (!File.Exists(swfFile))
            {
                string errMsg = swfFile + " が見つかりません。";
                throw new Exception(errMsg);
            }

            string parameterString = "-b 14 " + swfFile + " -o " + pngFile;
            Console.WriteLine("Command Parameter for tool: {0}", parameterString);
            ProcessStartInfo psInfo = new ProcessStartInfo(toolFile, parameterString);
            Process p = Process.Start(psInfo);  // This may cause unhandled excaption.
            p.WaitForExit();
            File.Delete(swfFile);
            Console.WriteLine("Finished extracting png.");

            /* **************************************
             * auth1
             * **************************************/
            req = (System.Net.HttpWebRequest)WebRequest.Create(auth1URL);
            req.Headers.Add("pragma: no-cache");
            req.Headers.Add("X-Radiko-App: pc_1");
            req.Headers.Add("X-Radiko-App-Version: 2.0.1");
            req.Headers.Add("X-Radiko-User: test-stream");
            req.Headers.Add("X-Radiko-Device: pc");
            req.Method = "POST";

            Console.WriteLine("Request to: {0}", req.RequestUri);
            try
            {
                res = (System.Net.HttpWebResponse)req.GetResponse();
            }
            catch (Exception e)
            {
                throw;
            }
            Console.WriteLine("HTTP Status Code: {0}", res.StatusCode.ToString());

            string authToken = "";
            int keyLength = 0;
            int keyOffset = 0;

            if (res.StatusCode == HttpStatusCode.OK)
            {
                // retrieve AuthToken, KeyLength, KeyOffset from response headers.
                for (int i = 0; i < res.Headers.Count; i++)
                {
                    Console.WriteLine("{0} : {1}", res.Headers.GetKey(i).ToString(), res.Headers[i].ToString());
                    switch (res.Headers.GetKey(i).ToString().ToLower())
                    {
                        case "x-radiko-authtoken":
                            authToken = res.Headers[i].ToString();
                            break;

                        case "x-radiko-keylength":
                            keyLength = Int32.Parse(res.Headers[i]);
                            break;

                        case "x-radiko-keyoffset":
                            keyOffset = Int32.Parse(res.Headers[i]);
                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                string errMsg = req.RequestUri + " がHTTP状態コード " + res.StatusCode.ToString() + " を返しました。";
                throw new Exception(errMsg);
            }

            // verify values
            if (authToken.Length == 0 || keyLength == 0 || keyOffset == 0)
            {
                string errMsg = "HTTPヘッダー情報に異常があります。";
                throw new Exception(errMsg);
            }

            Console.WriteLine("Finished auth1.");

            /* **************************************
             * get partial key
             * **************************************/
            // Verify file exists
            if (!File.Exists(pngFile))
            {
                string errMsg = pngFile + " が見つかりません。";
                throw new Exception(errMsg);
            }

            FileStream fs2 = new FileStream(pngFile, FileMode.Open, FileAccess.Read);   // This may cause unhandled exception
            byte[] buf1 = new byte[keyOffset];
            byte[] buf2 = new byte[keyLength];
            fs2.Read(buf1, 0, (int)(keyOffset));
            fs2.Read(buf2, 0, (int)keyLength); 
            string partialKey = System.Convert.ToBase64String(buf2);
            fs2.Close();
            File.Delete(pngFile);
            Console.WriteLine("Finished getting a partial key.");

            /* **************************************
             * auth2
             * **************************************/
            req = (System.Net.HttpWebRequest)WebRequest.Create(auth2URL);
            req.Headers.Add("pragma: no-cache");
            req.Headers.Add("X-Radiko-App: pc_1");
            req.Headers.Add("X-Radiko-App-Version: 2.0.1");
            req.Headers.Add("X-Radiko-User: test-stream");
            req.Headers.Add("X-Radiko-Device: pc");
            req.Headers.Add("X-Radiko-AuthToken: " + authToken);
            req.Headers.Add("X-Radiko-Partialkey: " + partialKey);
            req.Method = "POST";

            Console.WriteLine("Request to: {0}", req.RequestUri);
            try
            {
                res = (System.Net.HttpWebResponse)req.GetResponse();
            }
            catch (Exception e)
            {
                throw;
            }
            Console.WriteLine("HTTP Status Code: {0}", res.StatusCode.ToString());

            if (res.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("{0},{1}", authToken, new String('0', 32)); // string after "," is dummy.
            }
            else
            {
                string errMsg = req.RequestUri + " がHTTP状態コード " + res.StatusCode.ToString() + " を返しました。";
                throw new Exception(errMsg);
            }

            return;
        }
    }
}
