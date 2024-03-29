using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;



if (!Directory.Exists("Server\\data\\"))
{
    Directory.CreateDirectory("Server\\data\\");
}
if (!File.Exists("indexes.txt"))
{
    File.Create("indexes.txt");
}
IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
using Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
server.Bind(ipPoint);
server.Listen();
Console.WriteLine($"Server started!");
Hashtable filesTable = new Hashtable();
int voidId=1;
putHashTable(ref filesTable,ref voidId);
List<Socket> clients = new List<Socket>();
bool Started = true;
try
{
    while (Started) // Цикл для ожидания новых подключений
    {
        try
        {
            Socket client = await server.AcceptAsync();
            clients.Add(client);
            Thread t = new Thread(async () => ProcessClientAsync(client));
            t.Start();
        }
        catch
        {
            break;
        }
    }
}
finally
{
    getHashTable(filesTable);
    server.Close();
}
    async Task ProcessClientAsync(Socket client)
{
    Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");

    byte[] b = new byte[512];
    var files = new List<(int, string)>();
    try
    {
        while (true)
        {
            string temp = "";
            client.Receive(b);
            string msg = Encoding.Default.GetString(b);
            var zpr = msg.Split("`");
            if (zpr[0].Contains("exit"))
            {
                client.Close();
                break;
            }
            else if (zpr[0] == "1")
            {
                byte[] resp = new byte[Convert.ToInt32(zpr[3])]; client.Receive(resp);
                PUT(client, zpr[2], filesTable, resp, zpr[4]);
            }
            else if (zpr[0] == "2")
            {
                if (zpr[1] == "1")
                {
                    GET(zpr[2],client);
                }
                else if (zpr[1] == "2")
                {
                    GETbyId(zpr[2], filesTable,client);
                }
            }
            else
            {
                if (zpr[1] == "1")
                {
                    var res = DELETE(zpr[2], ref filesTable);
                    await client.SendAsync(Encoding.UTF8.GetBytes(res));
                }
                else
                {
                    var res = DELETEbyID(zpr[2], ref filesTable);
                    await client.SendAsync(Encoding.UTF8.GetBytes(res));
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("Клиент отключился ");
        clients.Remove(client);
        if (clients.Count == 0)
        {
            Started = false;
            getHashTable(filesTable);
            server.Close();
        }
    }
    finally
    {
        client.Close();
    }

}

async void PUT(Socket client,string fileName,Hashtable a, byte[] responseBytes,string FileFormat)
{
    if (fileName != "")
    {
        if (a.Contains(fileName))
        {
            client.SendAsync(Encoding.UTF8.GetBytes("403"));
        }
        else
        {

            client.SendAsync(Encoding.UTF8.GetBytes("202`" + voidId.ToString()));
            using (var file = File.Open("Server\\data\\" + fileName, FileMode.CreateNew, FileAccess.Write))
            {
                file.Write(responseBytes);
            }
            a.Add( voidId.ToString(),fileName);
            voidId=a.Count+1;
        }
    }
    else
    {

        client.SendAsync(Encoding.UTF8.GetBytes("202`" + voidId.ToString()));
        await using (var file = File.Open("Server\\data\\" + voidId + FileFormat.Split(".")[1], FileMode.CreateNew, FileAccess.Write))
        {
            file.Write(responseBytes);
        }
        a.Add(voidId.ToString(), voidId + FileFormat.Split(".")[1]);
        voidId = a.Count + 1;
    }
}

void putHashTable(ref Hashtable a,ref int voidId)
{
    using(StreamReader reader =new StreamReader("indexes.txt"))
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            a.Add(line.Split(" ")[0], line.Split(" ")[1]);
            voidId++;
        }
    }
}


void getHashTable(Hashtable a)
{
    using(StreamWriter writer = new StreamWriter("indexes.txt",false))
    {
        ICollection keys = a.Keys.Cast<string>().OrderBy(c=>c).ToArray();
        foreach (string s in keys)
        {
            writer.WriteLine(s+" " + a[s]);
        }
    }
}
async void GET(string fileName,Socket client)
{
    if (!File.Exists("Server\\data\\" + fileName))
    {
        await client.SendAsync(Encoding.UTF8.GetBytes("404"));
    }
    else
    {

        var e = new FileInfo("Server\\data\\" + fileName).Length;
        var temp = File.ReadAllBytes("Server\\data\\" + fileName);
        await client.SendAsync(Encoding.UTF8.GetBytes("200`"+e));
        await client.SendAsync(temp);
    }
}


async void GETbyId(string fileID,Hashtable a, Socket client)
{
    if (!a.ContainsKey(fileID))
    {
        await client.SendAsync(Encoding.UTF8.GetBytes("404"));
    }
    else
    {
        var e = new FileInfo("Server\\data\\" + a[fileID]).Length;
        var temp = File.ReadAllBytes("Server\\data\\" + a[fileID]);
        await client.SendAsync(Encoding.UTF8.GetBytes("200`" + e));
        await client.SendAsync(temp);
    }
}
string DELETE(string fileName, ref Hashtable a)
{
    if (!File.Exists("Server\\data\\" + fileName))
    {
        return "404";
    }
    else
    {
        File.Delete("Server\\data\\" + fileName);
        foreach (var key in a.Keys)
        {
            if (a[key]==fileName)
            {
                a.Remove(key);
                break;
            }
        }
        return "200";
    }
}

string DELETEbyID(string fileID,ref Hashtable a)
{
    if (!a.ContainsKey(fileID))
    {
        return "404";
    }
    else
    {
        File.Delete("Server\\data\\" + a[fileID]);
        a.Remove(fileID);
        return "200";
    }
}