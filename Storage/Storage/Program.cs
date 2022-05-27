using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Storage
{
    static class Program
    {
        public const string folderPath = "storage/";
        public const string URL = "http://localhost:8000/";

        public static bool CheckPath(string localPath)
        {
            if (localPath.Length == 0)
            {
                Console.WriteLine("400 Bad Request");
                return false;
            }

            return true;
        }
        private static void ProcessPut(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            bool bad = false;
            string localPath = folderPath + httpRequest.Url.LocalPath.Substring(1); ;
            if (!CheckPath(localPath))
            {
                bad = true;
            }
            else
            {
                int length = localPath.LastIndexOf("/");
                if (length >= 0)
                {
                    string filePath = localPath.Substring(0, length);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }

                    if (Directory.Exists(localPath))
                    {
                        bad = true;
                    }
                    else
                    {
                        using (var input = httpRequest.InputStream)
                        {
                            FileStream fileStream = File.Create(localPath);
                            input.CopyTo(fileStream);
                            fileStream.Close();
                        }
                    }
                }
                else
                {
                    bad = true;
                }
            }

            if(bad)
            {
                httpResponse.StatusCode = 400;
                using (var output = httpResponse.OutputStream)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes("400 Bad Request");
                    output.Write(buffer, 0, buffer.Length);
                }
            }
            Console.WriteLine(httpResponse.StatusCode.ToString());
        }

        private static void ProcessGet(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            bool bad = false;

            string localPath = folderPath + httpRequest.Url.LocalPath.Substring(1);
            if (!CheckPath(localPath))
            {
                bad = true;
            }

            FileInfo file = new FileInfo(localPath);
            if (file.Exists)
            {
                using (var output = httpResponse.OutputStream)
                {
                    httpResponse.ContentLength64 = file.Length;
                    byte[] buffer = File.ReadAllBytes(localPath);
                    output.Write(buffer, 0, buffer.Length);
                }
            }
            else if (Directory.Exists(localPath))
            {
                string[] dirs = Directory.GetDirectories(localPath);
                string[] files = Directory.GetFiles(localPath);
                var jsonInfo = JsonConvert.SerializeObject(files);
                var jsonInfoDirs = JsonConvert.SerializeObject(dirs);

                using (var output = httpResponse.OutputStream)
                {
                    var buffer = Encoding.ASCII.GetBytes(jsonInfo + '\n' + jsonInfoDirs);
                    output.Write(buffer, 0, buffer.Length);
                }
            }
            else
            {
                bad = true;
            }

            if(bad)
            {
                httpResponse.StatusCode = 404;
                using (var output = httpResponse.OutputStream)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes("404 Not Found");
                    output.Write(buffer, 0, buffer.Length);
                }
            }

            Console.WriteLine(httpResponse.StatusCode.ToString());
        }

        private static void ProcessDelete(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            bool bad = false;

            string localPath = folderPath + httpRequest.Url.LocalPath.Substring(1);
            if (!CheckPath(localPath))
            {
                bad = true;
            }

            FileInfo file = new FileInfo(localPath);
            
            if (file.Exists)
            {
                File.Delete(localPath);
            }
            else if (Directory.Exists(localPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(localPath);
                foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                {
                    fileInfo.Delete();
                }

                foreach (DirectoryInfo dirInfo in directoryInfo.GetDirectories())
                {
                    dirInfo.Delete(true);
                }

                Directory.Delete(localPath);
            }
            else
            {
                bad = true;
            }

            if (bad)
            {
                httpResponse.StatusCode = 400;
                using (var output = httpResponse.OutputStream)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes("400 Bad Request");
                    httpResponse.ContentLength64 = buffer.Length;
                    output.Write(buffer, 0, buffer.Length);
                }
            }

            Console.WriteLine(httpResponse.StatusCode);

        }

        private static void ProcessHead(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            bool bad = false;

            string localPath = folderPath + httpRequest.Url.LocalPath.Substring(1);
            Console.WriteLine(localPath);
            if (!CheckPath(localPath))
            {
                bad = true;
            }

            FileInfo fileToGetInfo = new FileInfo(localPath);
            if (fileToGetInfo.Exists)
            {
                httpResponse.Headers.Add("Size: " + fileToGetInfo.Length);
            }
            else
            {
                bad = true;
            }


            if (bad)
            {
                httpResponse.StatusCode = 404;
                using (var output = httpResponse.OutputStream)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes("404 Not Found");
                    httpResponse.ContentLength64 = buffer.Length;
                    output.Write(buffer, 0, buffer.Length);
                }
            }

            Console.WriteLine(httpResponse.StatusCode);

        }

        private static void ProcessCopy(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            bool bad = false;

            string localPath = folderPath + httpRequest.Url.LocalPath.Substring(1);
            string copyPath = httpRequest.Headers[0];
            copyPath = folderPath + copyPath.Replace(URL.Substring(5), "");
            if (!CheckPath(localPath) || !CheckPath(localPath))
            {
                bad = true;
            }

            FileInfo fileToGetInfo = new FileInfo(localPath);
            if (fileToGetInfo.Exists && Directory.Exists(copyPath))
            {
                try
                {
                    File.Copy(localPath, copyPath + localPath.Substring(localPath.LastIndexOf("/") + 1), true);
                }
                catch
                {
                    bad |= true;
                }
            }
            else
            {
                bad = true;
            }

            if(bad)
            {
                httpResponse.StatusCode = 400;
                using (var output = httpResponse.OutputStream)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes("400 Bad Request");
                    httpResponse.ContentLength64 = buffer.Length;
                    output.Write(buffer, 0, buffer.Length);
                }
            }

            Console.WriteLine(httpResponse.StatusCode);
        }

        private static void ProcessRequests(HttpListener listener)
        {
            while (true)
            {
                HttpListenerContext httpContext = listener.GetContext();
                HttpListenerRequest httpRequest = httpContext.Request;
                HttpListenerResponse httpResponse = httpContext.Response;

                Console.WriteLine(httpRequest.HttpMethod);

                switch (httpRequest.HttpMethod)
                {
                    case "GET":
                        {
                            ProcessGet(httpRequest, httpResponse);
                            break;
                        }

                    case "PUT":
                        {
                            ProcessPut(httpRequest, httpResponse);
                            break;
                        }

                    case "HEAD":
                        {
                            ProcessHead(httpRequest, httpResponse);
                            break;
                        }

                    case "DELETE":
                        {
                            ProcessDelete(httpRequest, httpResponse);
                            break;
                        }

                    case "COPY":
                        {
                            ProcessCopy(httpRequest, httpResponse);
                            break;
                        }
                }
                Console.WriteLine();
                httpResponse.Close();
            }
        }

        public static void Main()
        {
            Console.WriteLine("Welcome to my storage! (ready to handle requests)");
            HttpListener httpListener;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(URL);
            httpListener.Start();
            ProcessRequests(httpListener);
            httpListener.Close();
        }
    }
}