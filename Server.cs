using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class Server
{
    static TcpListener listener;
    static List<ClientHandler> clients = new List<ClientHandler>();
    static object lockObj = new object();

    static void Main(string[] args)
    {
        Console.WriteLine("=== Chat Server ===");
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Server started on port 5000...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            ClientHandler handler = new ClientHandler(client);
            lock (lockObj) clients.Add(handler);
            new Thread(handler.Run).Start();
        }
    }

    public static void Broadcast(string message, ClientHandler excludeClient = null)
    {
        lock (lockObj)
        {
            foreach (var client in clients)
            {
                if (client != excludeClient)
                    client.SendMessage(message);
            }
        }
    }

    public static void RemoveClient(ClientHandler client)
    {
        lock (lockObj) clients.Remove(client);
    }
}

class ClientHandler
{
    TcpClient client;
    NetworkStream stream;
    string userName;

    public ClientHandler(TcpClient client)
    {
        this.client = client;
        this.stream = client.GetStream();
    }

    public void Run()
    {
        try
        {
            byte[] buffer = new byte[1024];
            int count = stream.Read(buffer, 0, buffer.Length);
            userName = Encoding.UTF8.GetString(buffer, 0, count);
            Console.WriteLine($"{userName} connected.");
            Server.Broadcast($"{userName} joined the chat.", this);

            while (true)
            {
                count = stream.Read(buffer, 0, buffer.Length);
                if (count == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, count);

                if (message == "/exit")
                    break;

                // приватне повідомлення: @ім’я текст
                if (message.StartsWith("@"))
                {
                    string[] parts = message.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        string targetName = parts[0].Substring(1);
                        string privateMsg = $"{userName} (private): {parts[1]}";
                        SendPrivate(targetName, privateMsg);
                        continue;
                    }
                }

                Console.WriteLine($"{userName}: {message}");
                Server.Broadcast($"{userName}: {message}", this);
            }
        }
        catch { }
        finally
        {
            Console.WriteLine($"{userName} disconnected.");
            Server.Broadcast($"{userName} left the chat.", this);
            Server.RemoveClient(this);
            stream.Close();
            client.Close();
        }
    }

    void SendPrivate(string targetName, string message)
    {
        lock (typeof(Server))
        {
            foreach (var c in typeof(Server)
                .GetField("clients", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null) as List<ClientHandler>)
            {
                if (c.userName == targetName)
                {
                    c.SendMessage(message);
                    return;
                }
            }
        }
    }

    public void SendMessage(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
        catch { }
    }
}
