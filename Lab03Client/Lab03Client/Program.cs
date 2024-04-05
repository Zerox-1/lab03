using System;
using System.Net.Sockets;
using System.Text;

if (!Directory.Exists("client\\data\\"))
{
    Directory.CreateDirectory("client\\data\\");
}
var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
try
{
    await client.ConnectAsync("127.0.0.1", 8888);
    var responseBytes = new byte[8192];
    int bytes=0;
    Console.WriteLine($"Подключение к {client.RemoteEndPoint} установлено");
    string response = "";
    string temp2 = "";
    string temp;
    while (true)
    {
        temp = getOperation();
        if (temp == "exit")
        {
            client.Close();
            break;
        }
        else if (temp == "2")
        {
            Console.WriteLine("Do you want to get the file by name or by id (1 - name, 2 - id): >");
            temp2 = Console.ReadLine();
            if (temp2 == "1")
            {
                Console.WriteLine("Enter name: >");
                temp2 = Console.ReadLine();
                client.Send(Encoding.UTF8.GetBytes("2`1`" + temp2 + "`"));
            }
            else
            {
                Console.WriteLine("Enter ID: >");
                temp2 = Console.ReadLine();
                client.Send(Encoding.UTF8.GetBytes("2`2`" + temp2 + "`"));
            }
            bytes = await client.ReceiveAsync(responseBytes);
            response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
            var items = response.Split("`");
            if (items[0] == "200")
            {
                var bytes1 = new byte[Convert.ToInt32(items[1])];
                await client.ReceiveAsync(bytes1);
                Console.WriteLine("The File was downloaded! Specify a name for it: >");
                string tempQ = Console.ReadLine();
                using (var file = File.Open("client\\data\\" + tempQ, FileMode.CreateNew, FileAccess.Write))
                {
                    file.Write(bytes1);
                }
            }
            else
            {
                Console.WriteLine("The response says that this file is not found!");
            }
        }
        else if (temp == "3")
        {
            Console.WriteLine("Do you want to delete the file by name or by id (1 - name, 2 - id): >");
            temp2 = Console.ReadLine();
            if (temp2 == "1")
            {
                Console.WriteLine("Enter name: >");
                temp2 = Console.ReadLine();
                Console.WriteLine("The request was sent.");
                client.Send(Encoding.UTF8.GetBytes("3`1`" + temp2+"`"));

            }
            else
            {
                Console.WriteLine("Enter ID: >");
                temp2 = Console.ReadLine();
                Console.WriteLine("The request was sent.");
                client.Send(Encoding.UTF8.GetBytes("3`2`" + temp2 + "`"));
            }
            bytes = await client.ReceiveAsync(responseBytes);
            response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
            var items = response.Split("`");
            if (items[0] == "404")
            {
                Console.WriteLine("The response says that this file is not found!");
            }
            else
            {
                Console.WriteLine("The response says that this file was deleted successfully!");
            }
        }
        else
        {
            Console.WriteLine("Enter name of the file: >");
            temp2 = Console.ReadLine();
            while (true)
            {
                if (File.Exists("client\\data\\" + temp2))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("File dosn`t exists");
                    Console.WriteLine("Enter name of the file: >");
                    temp2 = Console.ReadLine();
                }
            }
            Console.WriteLine("Enter name of the file to be saved on server: >");
            string temp3 = Console.ReadLine();
            Console.WriteLine("The request was sent.");
            saveFile(temp2, temp3, client, responseBytes, bytes, response);
        }
    }
}
catch
{
    Console.WriteLine($"Не удалось установить подключение");
}
finally
{
    client.Close();
}


string getOperation()
{
    string temp;
    Console.WriteLine("Enter action (1-put in a file,2 - get a file,3 delete a file,exit-for exit):>");
    temp = Console.ReadLine();
    while (true)
    {
        if (temp == "1" || temp == "2" || temp == "3" || temp=="exit" )
        {
            break;
        }
        else
        {
            Console.WriteLine("Введите корректное значение! \n Enter action (1-put in a file,2 - get a file,3 delete a file,exit-for exit):>");
            temp = Console.ReadLine();
        }
    }
    return temp;
}


async void saveFile(string fileName, string serverFile, Socket client, byte[] responseBytes, int bytes, string response)
{   var e = new FileInfo("client\\data\\" + fileName).Length;
    var msg = "1`1`" + serverFile + "`" + e + "`"+fileName+"`";
    var msg1 = File.ReadAllBytes("client\\data\\" + fileName);
    await client.SendAsync(Encoding.UTF8.GetBytes(msg));
    await client.SendAsync(msg1);
    bytes = client.Receive(responseBytes);
    response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
    var items = response.Split("`");
    if (items[0].Contains("202"))
    {
        Console.WriteLine("Response says that file is saved! ID =" + items[1]);
    }
}