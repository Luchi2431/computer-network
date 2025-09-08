// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Hello, World!");

//kreiranje uticnice za slanje podataka
Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

//Podesavanje adrese primaoca
IPEndPoint recvEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27015);

//Poruka za slanje
string meesage = "Ide gas";
byte[] messageBytes = Encoding.UTF8.GetBytes(meesage);

try
{
    int bytesSent = sendSocket.SendTo(messageBytes, 0, messageBytes.Length, SocketFlags.None, recvEndPoint);
    System.Console.WriteLine("Sent {0} bytes to {1}", bytesSent, recvEndPoint);
}
catch (System.Exception ex)
{
    System.Console.WriteLine("sendto failed with error: {0}", ex.Message);
    throw;
}
finally
{
    //zatvaranje uticnice nakon slanja
    sendSocket.Close();
}
