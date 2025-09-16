using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    static void Main(string[] args)
    {
        Console.WriteLine("Enter your name:");
        string name = Console.ReadLine();

        TcpClient client = new TcpClient("127.0.0.1", 5000);
        NetworkStream stream = client.GetStream();

        byte[] nameData = Encoding.UTF8.GetBytes(name);
        stream.Write(nameData, 0, nameData.Length);

        Thread receiveThread = new Thread(() =>
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int count = stream.Read(buffer, 0, buffer.Length);
                    if (count == 0) break;
                    string message = Encoding.UTF8.GetString(buffer, 0, count);
                    Console.WriteLine(message);
                }
            }
            catch { }
        });
        receiveThread.Start();

        while (true)
        {
            string msg = Console.ReadLine();
            if (msg == "/exit")
            {
                byte[] exitData = Encoding.UTF8.GetBytes("/exit");
                stream.Write(exitData, 0, exitData.Length);
                break;
            }
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
        }

        stream.Close();
        client.Close();
    }
}
