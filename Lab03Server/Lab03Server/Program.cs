using System.Net.Sockets;
using System.Net;
using System.Text;

IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
using Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
server.Bind(ipPoint);
server.Listen();
Console.WriteLine($"Server started!");
try
{
    while (true) // Цикл для ожидания новых подключений
    {
        using Socket client = await server.AcceptAsync();
        Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");
        byte[] b = new byte[512];
        var files = new List<(int, string)>();
        try
        {
            string temp = "";
            client.Receive(b);
            string msg = Encoding.Default.GetString(b);
            string[] zpr = new string[3];
            zpr = msg.Split("`");
            if (zpr[0].Contains("exit"))
            {
                client.Send(Encoding.UTF8.GetBytes("1"));
                break;
            }
            else if (zpr[0] == "1")
            {
                var res = PUT(zpr[1], zpr[2]);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
            }
            else if (zpr[0] == "2")
            {
                var res = GET(zpr[1]);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
            }
            else
            {
                var res = DELETE(zpr[1]);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
            }
            client.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Клиент отключился ");
        }
        finally
        {

            client.Close();
        }
    }
}
finally
{
    server.Close();
}


string PUT(string fileName,string text)
{
    if (File.Exists("Server\\data\\" + fileName))
    {
        return "403";
    }
    else
    {
        using(var stream= File.Open("Server\\data\\" + fileName, FileMode.CreateNew,FileAccess.Write))
        {
            stream.Write(Encoding.UTF8.GetBytes(text));
        }
        return "202";
    }
}

string GET(string fileName)
{
    if (!File.Exists("Server\\data\\" + fileName))
    {
        return "404";
    }
    else
    {
        string temp = File.ReadAllText("Server\\data\\" + fileName);
        return "200`"+temp;
    }
}
string DELETE(string fileName)
{
    if (!File.Exists("Server\\data\\" + fileName))
    {
        return "404";
    }
    else
    {
        File.Delete("Server\\data\\" + fileName);
        return "200";
    }
}