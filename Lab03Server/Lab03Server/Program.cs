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
try
{
    while (true) // Цикл для ожидания новых подключений
    {
        Socket client =await server.AcceptAsync();
        Thread t = new Thread(async () => ProcessClientAsync(client));
        t.Start();
    }
}
finally
{
    server.Close();
}


async Task ProcessClientAsync(Socket client)
{
    Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");
    byte[] b = new byte[512];
    var files = new List<(int, string)>();
    try
    {
        string temp = "";
        client.Receive(b);
        string msg = Encoding.Default.GetString(b);
        var zpr = msg.Split("`");
        if (zpr[0].Contains("exit"))
        {
            client.Close();
        }
        else if (zpr[0] == "1")
        {
            byte[] resp = new byte[Convert.ToInt32(zpr[3])]; client.Receive(resp);
            if (zpr[1].Contains("txt"))
            {
                PUT(client, zpr[2], filesTable, resp);
            }
            else
            {
                PUT_image(client, zpr[2], filesTable, resp);
            }
            getHashTable(filesTable);
        }
        else if (zpr[0] == "2")
        {
            if (zpr[1] == "1")
            {
                var res = GET(zpr[2]);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
            }
            else if (zpr[1]=="2")
            {
                var res = GETbyId(zpr[2], filesTable);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
            }
            else if (zpr[1] == "3")
            {
                var res = GET_image(zpr[2]);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
                if (res.Contains("200"))
                {
                    using (var ms = new MemoryStream())
                    {
                        Image image = Image.FromFile("Server\\data\\" + zpr[2]);
                        image.Save(ms, image.RawFormat);
                        await client.SendAsync(ms.ToArray());
                    }
                }
            }
            else if (zpr[1] == "4")
            {
                var res = GET_imagebyId(zpr[2], filesTable);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
                if (res.Contains("200"))
                {
                    using (var ms = new MemoryStream())
                    {
                        Image image = Image.FromFile("Server\\data\\" + filesTable[zpr[2]]);
                        image.Save(ms, image.RawFormat);
                        await client.SendAsync(ms.ToArray());
                    }
                }
            }
        }
        else
        {
            if (zpr[1] == "1")
            {
                var res = DELETE(zpr[2], ref filesTable);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
                getHashTable(filesTable);
            }
            else
            {
                var res = DELETEbyID(zpr[2], ref filesTable);
                await client.SendAsync(Encoding.UTF8.GetBytes(res));
                getHashTable(filesTable);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("Клиент отключился ");
    }
    finally
    {

        client.Close();
        Console.WriteLine("Клиент отключился ");
    }
}

async void PUT(Socket client,string fileName,Hashtable a, byte[] responseBytes)
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
        await using (var file = File.Open("Server\\data\\" + voidId + ".txt", FileMode.CreateNew, FileAccess.Write))
        {
            file.Write(responseBytes);
        }
        a.Add(voidId.ToString(),voidId + ".txt" );
        voidId = a.Count + 1;
    }
}


async void PUT_image(Socket client, string fileName, Hashtable a, byte[] responseBytes)
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
            using (var ms = new MemoryStream(responseBytes))
            {
                Image img=Image.FromStream(ms);
                img.Save("Server\\data\\" + fileName,ImageFormat.Png);
                ms.Close();
            }
            a.Add(voidId.ToString(), fileName);
            voidId = a.Count + 1;
        }
    }
    else
    {

        client.SendAsync(Encoding.UTF8.GetBytes("202`" + voidId.ToString()));
        using (var ms = new MemoryStream(responseBytes))
        {
            Image img = Image.FromStream(ms);
            img.Save("Server\\data\\" + voidId + ".png", ImageFormat.Png);
            ms.Close();
        }
        a.Add(voidId.ToString(), voidId + ".png");
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

string GET_image(string fileName)
{
    if (!File.Exists("Server\\data\\" + fileName))
    {
        return "404";
    }
    else
    {
        long e;
        byte[] ms1;
        using (var ms = new MemoryStream())
        {
            Image image = Image.FromFile("Server\\data\\" + fileName);
            image.Save(ms, image.RawFormat);
            e = ms.Length;
            ms1 = ms.ToArray();
            ms.Close();
        }
        return "200`"+ms1.Length+"`";
    }
}
string GET_imagebyId(string fileID, Hashtable a)
{
    if (!a.ContainsKey(fileID))
    {
        return "404";
    }
    else
    {
        long e;
        byte[] ms1;
        using (var ms = new MemoryStream())
        {
            Image image = Image.FromFile("Server\\data\\" + a[fileID]);
            image.Save(ms, image.RawFormat);
            e = ms.Length;
            ms1 = ms.ToArray();
            ms.Close();
        }
        return "200`" + ms1.Length + "`";
    }
}

string GETbyId(string fileID,Hashtable a)
{
    if (!a.ContainsKey(fileID))
    {
        return "404";
    }
    else
    {
        string temp = File.ReadAllText("Server\\data\\" + a[fileID]);
        return "200`" + temp;
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
                voidId = Convert.ToInt16(key);
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
        voidId = Convert.ToInt16(fileID);
        return "200";
    }
}