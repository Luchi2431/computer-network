// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;

class Zaposleni
{
    private const string ZAPOSLENI_FILE = "zaposleni.txt";
    private const int UDP_PORT = 9000;
    static void Main()
    {
        string username = GetUsername();
        //Kreiranje udp uticnice
        Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //Podesavanje adrese primaoca poruke koje kreiramo
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 9000);

        //Slanje poruke na server
        string messageToSend = $"ZAPOSLENI:{username}";

        if (messageToSend.StartsWith("ZAPOSLENI:"))
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
            udpSocket.SendTo(messageBytes, serverEP);
        }

        //Cekaj odgovor od servera
        byte[] recvBuffer = new byte[1024];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        int received = udpSocket.ReceiveFrom(recvBuffer, ref remoteEP);
        string response = Encoding.UTF8.GetString(recvBuffer, 0, received);

        System.Console.WriteLine($"Server odgovorio: {response}");

        if (response.StartsWith("TCP_PORT:"))
        {
            int tcpPort = int.Parse(response.Split(":")[1]);

            //TCP deo
            Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            System.Console.WriteLine("Zaposleni povezan preko TCP-a sa Serverom");
        }
    }

    static string GetUsername()
    {
        string imeZaposlenog = string.Empty;
        if (File.Exists(ZAPOSLENI_FILE))
        {
            imeZaposlenog = File.ReadAllText(ZAPOSLENI_FILE);
            System.Console.WriteLine($"Dobrodosli nazad: {imeZaposlenog}");
            return imeZaposlenog;
        }
        System.Console.WriteLine("Unesite ime zaposlenog");
        imeZaposlenog = Console.ReadLine() ?? string.Empty;
        File.WriteAllText(ZAPOSLENI_FILE, imeZaposlenog);
        return imeZaposlenog;
    }

}