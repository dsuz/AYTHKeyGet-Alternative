using System;
//using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Diagnostics;

namespace AYTHKeyGet
{
    class Program
    {
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
            // Verify swfextract
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
            // Build request and request
            HttpWebRequest req =  (System.Net.HttpWebRequest) WebRequest.Create(playerURL);
            Console.WriteLine("Request to: {0}", req.RequestUri);
            HttpWebResponse res = (System.Net.HttpWebResponse) req.GetResponse();   // This may cause unhandled exception.

            // Check response and save received swf file.
            Console.WriteLine("HTTP Status Code: {0}", res.StatusCode.ToString());
            if (res.StatusCode == HttpStatusCode.OK)
            {
                // Save received swf file in response
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

            // Build command and run
            string parameterString = "-b 14 " + swfFile + " -o " + pngFile;
            Console.WriteLine("Command Parameter for tool: {0}", parameterString);
            try
            {
                ProcessStartInfo psInfo = new ProcessStartInfo(toolFile, parameterString);
                Process p = Process.Start(psInfo);
                p.WaitForExit();
                File.Delete(swfFile);
            }
            catch (Exception e)
            {
                throw;
            }
            Console.WriteLine("Finished extracting png.");

            /* **************************************
             * auth1
             * **************************************/
            // Build request
            req = (System.Net.HttpWebRequest)WebRequest.Create(auth1URL);
            req.Headers.Add("pragma: no-cache");
            req.Headers.Add("X-Radiko-App: pc_1");
            req.Headers.Add("X-Radiko-App-Version: 2.0.1");
            req.Headers.Add("X-Radiko-User: test-stream");
            req.Headers.Add("X-Radiko-Device: pc");
            req.Method = "POST";

            // Request
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

            // Retrieve AuthToken, KeyLength, KeyOffset from response headers.
            string authToken = "";
            int keyLength = 0;
            int keyOffset = 0;

            if (res.StatusCode == HttpStatusCode.OK)
            {
                // Scan headers
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

            // Verify values
            if (authToken.Length == 0 || keyLength == 0 || keyOffset == 0)
            {
                string errMsg = "HTTPヘッダー情報に異常があります。";
                throw new Exception(errMsg);
            }

            Console.WriteLine("Finished auth1.");

            /* **************************************
             * Get partial key
             * **************************************/
            // Verify file exists
            if (!File.Exists(pngFile))
            {
                string errMsg = pngFile + " が見つかりません。";
                throw new Exception(errMsg);
            }

            // Get partial key
            string partialKey = "";
            try
            {
                // Open png file, retrieve certain part of the binary, and encode in base64
                FileStream fs2 = new FileStream(pngFile, FileMode.Open, FileAccess.Read);
                byte[] buf1 = new byte[keyOffset];  // Dummy buffer
                byte[] buf2 = new byte[keyLength];
                fs2.Read(buf1, 0, (int)keyOffset);  // Forward cursor to the offset
                fs2.Read(buf2, 0, (int)keyLength);  // Read staring from offset by length
                partialKey = System.Convert.ToBase64String(buf2);   // Convert binary in base64
                Console.WriteLine("Partial key: {0}", partialKey);  // Show retrieved partial key
                fs2.Close();
                File.Delete(pngFile);
            }
            catch (Exception e)
            {
                throw;
            }
            Console.WriteLine("Finished getting a partial key.");

            /* **************************************
             * auth2
             * **************************************/
            // Build request
            req = (System.Net.HttpWebRequest)WebRequest.Create(auth2URL);
            req.Headers.Add("pragma: no-cache");
            req.Headers.Add("X-Radiko-App: pc_1");
            req.Headers.Add("X-Radiko-App-Version: 2.0.1");
            req.Headers.Add("X-Radiko-User: test-stream");
            req.Headers.Add("X-Radiko-Device: pc");
            req.Headers.Add("X-Radiko-AuthToken: " + authToken);
            req.Headers.Add("X-Radiko-Partialkey: " + partialKey);
            req.Method = "POST";

            // Request
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

            // Display authenticated authToken on success
            if (res.StatusCode == HttpStatusCode.OK)
            {
                // This is what I need
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
