﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxy
{
    class Program
    {

        static void Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080); 
            listener.Start();
            while (true) 
            {
                var client = listener.AcceptTcpClient();
                Thread thread = new Thread(() => RecvData(client)); 
                thread.Start();
            }
        }

        public static void RecvData(TcpClient client) 
        {
            NetworkStream browser = client.GetStream();
            byte[] buf;
            buf = new byte[16000];
            while (true) 
            {
                if (!browser.CanRead)
                    return;
                try
                {
                    browser.Read(buf, 0, buf.Length);
                }
                catch (IOException)
                {
                    return;
                }
                HTTPserv(buf, browser, client);
            }
        }

        public static void HTTPserv(byte[] buf, NetworkStream browser, TcpClient client)
        {
            try
            {
                string[] temp = Encoding.ASCII.GetString(buf).Trim().Split(new char[] { '\r', '\n' });
                
                string req = temp.FirstOrDefault(x => x.Contains("Host")); 
                req = req.Substring(req.IndexOf(":") + 2);
                string[] port = req.Trim().Split(new char[] { ':' }); 

                TcpClient server;
                if (port.Length == 2) 
                {
                    server = new TcpClient(port[0], int.Parse(port[1]));
                }
                else
                {
                    server = new TcpClient(port[0], 80);
                }

                NetworkStream servStream = server.GetStream(); 
                servStream.Write(buf, 0, buf.Length);
                var respBuf = new byte[32]; 
                
               
                servStream.Read(respBuf, 0, respBuf.Length); 

                browser.Write(respBuf, 0, respBuf.Length); 

                string[] head = Encoding.UTF8.GetString(respBuf).Split(new char[] { '\r', '\n' }); 
         
                string ResponseCode = head[0].Substring(head[0].IndexOf(" ") + 1);
                Console.WriteLine($"\n{req} {ResponseCode}");
                servStream.CopyTo(browser); 

            }
            catch
            {
                return;
            }
            finally
            {
                client.Dispose();
            }

        }

    }

}

