using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace H3_Simple_WebServer
{
    class Server
    {
        public bool running = false;
        //will be used as milliseconds
        private int timeout = 8;
        private Encoding charEncoder = Encoding.UTF8;
        private Socket serverSocket; //
        private string contentPath; //Path to the content

        //Types that we support (content types)
        private Dictionary<string, string> extensions = new Dictionary<string, string>()
        {
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"}
        };


        public bool Start(IPAddress ipAddress, int port, int maxConn, string path)
        {
            if (running) return false;

            try
            {
                //InterNetwork for ipv4
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                               ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(ipAddress, port));
                //Queue length
                serverSocket.Listen(maxConn);
                serverSocket.ReceiveTimeout = timeout;
                serverSocket.SendTimeout = timeout;
                running = true;
                contentPath = path;
            }
            catch (Exception)
            {
                return false;
            }

            //Thread that will listen to requests
            //Creates new threads to handle the responses
            //Makes a anonymous method for the thread to run
            Thread listener = new Thread(() =>
            {
                while (running)
                {
                    Socket clientSocket;
                    try
                    {
                        //Accept the client and put it into clientSocket
                        clientSocket = serverSocket.Accept();
                        Thread requestHandler = new Thread(() =>
                        {
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            try { HandleRequest(clientSocket); }
                            catch
                            {
                                try { clientSocket.Close(); } catch { }
                            }
                        });
                        requestHandler.Start();
                    }
                    catch
                    {
                        //clientSocket = null;
                    }
                }
            });
            listener.Start();

            return true;
        }
        public void Stop()
        {
            if (running)
            {
                running = false;
                try
                {
                    serverSocket.Close();
                }
                catch
                {
                }
                serverSocket = null;
            }
        }

        private void HandleRequest(object cSocket)
        {
            Socket clientSocket = (Socket)cSocket;

            byte[] buffer = new byte[10240]; // 10 kb
            int byteAmount = clientSocket.Receive(buffer); // Fills the buffer array and byteAmount
            string responseString = charEncoder.GetString(buffer, 0, byteAmount);

            // Show if it was a GET or SET
            string requestAction = responseString.Substring(0, responseString.IndexOf(" "));

            int start = responseString.IndexOf(requestAction) + requestAction.Length + 1;
            int length = responseString.LastIndexOf("HTTP") - start - 1;
            string requestedUrl = responseString.Substring(start, length);

            string requestedFile;
            if (requestAction.Equals("GET") || requestAction.Equals("POST"))
                requestedFile = requestedUrl.Split('?')[0];
            else
            {
                NotImplemented(clientSocket);
                return;
            }

            requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");
            start = requestedFile.LastIndexOf('.') + 1;
            //Check if file has extension
            if (start > 0)
            {
                length = requestedFile.Length - start;
                string extension = requestedFile.Substring(start, length);
                //Do we support this extension?
                if (extensions.ContainsKey(extension))
                    //Check existence of the file
                    if (File.Exists(contentPath + requestedFile))
                        //Send requested file with correct content type: (MIME)
                        SendOk(clientSocket,
                            File.ReadAllBytes(contentPath + requestedFile), extensions[extension]);
                    else
                        NotFound(clientSocket);
            }
            else
            {
                // If file is not specified try to send index.htm or index.html
                if (requestedFile.Substring(length - 1, 1) != @"\")
                    requestedFile += @"\";
                if (File.Exists(contentPath + requestedFile + "index.htm"))
                    SendOk(clientSocket,
                      File.ReadAllBytes(contentPath + requestedFile + "\\index.htm"), "text/html");
                else if (File.Exists(contentPath + requestedFile + "index.html"))
                    SendOk(clientSocket,
                      File.ReadAllBytes(contentPath + requestedFile + "\\index.html"), "text/html");
                else
                    NotFound(clientSocket);
            }
        }

        private void NotImplemented(Socket clientSocket)
        {
            SendResponse(clientSocket, "<html><head>" +
                "<meta http - equiv =\"Content-Type\" content=\"text/html; charset = utf - 8\">" +
                "</ head >< body >< h2 > Simple_K_WebServer" +
                "Server </ h2 >< div > 501 - Method Not" +
                "Implemented </ div ></ body ></ html > ",
                "501 Not Implemented", "text/html");
        }

        private void NotFound(Socket clientSocket)
        {
            SendResponse(clientSocket, "<html><head><meta " +
                "http - equiv =\"Content-Type\" content=\"text/html;" +
                "charset = utf - 8\"></head><body><h2>Simple_K_WebServer" +
                "Server </ h2 >< div > 404 - Not Found </ div ></ body ></ html > ",
                "404 Not Found", "text/html");
        }

        private void SendOk(Socket clientSocket, byte[] bContent, string contentType)
        {
            SendResponse(clientSocket, bContent, "200 OK", contentType);
        }


        private void SendResponse(Socket clientSocket, string content, string responseCode, string contentType)
        {
            byte[] bContent = charEncoder.GetBytes(content);
            SendResponse(clientSocket, bContent, responseCode, contentType);
        }


        private void SendResponse(Socket clientSocket, byte[] content, string responseCode, string contentType)
        {
            try
            {
                byte[] bHeader = charEncoder.GetBytes(
                        "HTTP/1.1 " + responseCode + "\r\n"
                        + "Server: Simple_K_WebServer\r\n"
                        + "Content-Length: " + content.Length.ToString() + "\r\n"
                        + "Connection: close\r\n"
                        + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(content);
                clientSocket.Close();
            }
            catch
            {
            }
        }
    }
}
