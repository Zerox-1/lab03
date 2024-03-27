using System;
using System.Net.Sockets;
using System.Text;

var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
try
{
    await client.ConnectAsync("127.0.0.1", 8888);
    var responseBytes = new byte[512];
    int bytes;
    Console.WriteLine($"Подключение к {client.RemoteEndPoint} установлено");
    string response = "";
    string items = "";
    string temp;
    string msg;
    string zapros = "";
    Console.WriteLine("Enter action (1-put in a file,2 - get a file,3 delete a file,exit-for exit):>");
    temp = Console.ReadLine();
    while (true)
    {
        if (temp == "1" || temp == "2" || temp == "3")
        {
            zapros += temp + "`";
            break;
        }
        else if (temp == "exit")
        {
            client.Send(Encoding.UTF8.GetBytes(temp));
            bytes = await client.ReceiveAsync(responseBytes);
            response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
            if (response == "1") client.Close(); break;
        }
        else
        {
            Console.WriteLine("Введите корректное значение! \n Enter action (1-put in a file,2 - get a file,3 delete a file,exit-for exit):>");
            temp = Console.ReadLine();
        }
    }
    Console.WriteLine("Enter filename:>");
    zapros += Console.ReadLine() + "`";
    if (temp == "1")
    {
        Console.WriteLine("Enter  file content: >");
        zapros += Console.ReadLine();
        Console.WriteLine("The request was sent.");
        client.Send(Encoding.UTF8.GetBytes(zapros));
        bytes = await client.ReceiveAsync(responseBytes);
        response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
        if (response == "202")
        {
            Console.WriteLine("The response says that the file was created!");
        }
        else
        {
            Console.WriteLine("The response says that creating the file was forbidden!");
        }
    }
    else if (temp == "2")
    {
        Console.WriteLine("The request was sent.");
        client.Send(Encoding.UTF8.GetBytes(zapros));
        bytes = await client.ReceiveAsync(responseBytes);
        response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
        var temp1 = response.Split("`");
        if (response[0].ToString() == "2")
        {
            Console.WriteLine("The content of the file is: " + temp1[1]);
        }
        else
        {
            Console.WriteLine("The response says that the file was not found!");
        }
    }
    else
    {
        Console.WriteLine("The request was sent.");
        client.Send(Encoding.UTF8.GetBytes(zapros));
        bytes = await client.ReceiveAsync(responseBytes);
        response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
        if (response == "200")
        {
            Console.WriteLine("The response says that the file was successfully deleted!");
        }
        else
        {
            Console.WriteLine("The response says that the file was not found!");
        }
    }
    client.Close();
}
catch
{
    Console.WriteLine($"Не удалось установить подключение");
}
finally
{
    client.Close();
}