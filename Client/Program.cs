// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared;

class Manadzer
{
    static void Main()
    {
        string filePath = "menadzer.txt";
        string korisnickoIme;

        if (File.Exists(filePath))
        {
            korisnickoIme = File.ReadAllText(filePath);
            System.Console.WriteLine($"Dobrodosli nazad: {korisnickoIme}!");
        }
        else
        {
            System.Console.WriteLine("Unesite korisnicko ime:");
            korisnickoIme = Console.ReadLine() ?? string.Empty;
            File.WriteAllText(filePath, korisnickoIme);
        }

        int udpPort = 9000;
        //Kreiranje udp socketa za slanje poruke
        Socket udpClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //Podesavanje adrese primaoca
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, udpPort);

        //Kreiranje praznog baffera i priprema EP da u njega upisemo Adresu Servera
        byte[] buffer = new byte[1024];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        string message = $"MENADZER:{korisnickoIme}";
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        System.Console.WriteLine(message);
        udpClientSocket.SendTo(messageBytes, serverEP);

        //Prijem poruke sa servera
        int received = udpClientSocket.ReceiveFrom(buffer, ref remoteEP);
        string response = Encoding.UTF8.GetString(buffer, 0, received);
        System.Console.WriteLine($"Server je odgovorio sa porukom: {response}");

        if (response.StartsWith("TCP_PORT:"))
        {
            int tcpPort = int.Parse(response.Split(':')[1]);
            System.Console.WriteLine($"TCP Konekcija ce ici na portu: {tcpPort}.");
            //Kreiranje tcp uticnice za povezivanje
            Socket tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpClient.Connect(IPAddress.Loopback, tcpPort);

            //Kreiranje zadatka
            System.Console.WriteLine("Naziv zadatka");
            string naziv = Console.ReadLine() ?? string.Empty;
            System.Console.WriteLine("Zaposleni");
            string zaposleni = Console.ReadLine() ?? string.Empty;
            System.Console.WriteLine("Rok yyyy-mm-dd");
            string rok = Console.ReadLine() ?? string.Empty;
            System.Console.WriteLine("Prioritet (int)");
            string prioritet = Console.ReadLine() ?? string.Empty;

            string zadatakPoruka = $"{naziv}|{zaposleni}|{rok}|{prioritet}|{Status.NaCekanju}";
            tcpClient.Send(Encoding.UTF8.GetBytes(zadatakPoruka));

            System.Console.WriteLine("Zadatak poslat serveru");
            tcpClient.Close();
        }
        //PREGLED ZADATKA
        string pregledPoruke = $"PREGLED:{korisnickoIme}";
        udpClientSocket.SendTo(Encoding.UTF8.GetBytes(pregledPoruke), serverEP);

        received = udpClientSocket.ReceiveFrom(buffer, ref remoteEP);
        string tasksStr = Encoding.UTF8.GetString(buffer, 0, received);

        System.Console.WriteLine("\n Pregled zadataka 'U Toku':");
        //Razdeli string na delove i prikazi zadatke
        string[] lines = tasksStr.Split("\n");
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split("|");
            string naziv = parts[0];
            string zaposleni = parts[1];
            DateTime rok = DateTime.Parse(parts[2]);
            int prioritet = int.Parse(parts[3]);

            int daysLeft = (rok - DateTime.Now).Days;
            if (daysLeft < 2)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine($"!!!{naziv} za {zaposleni} (rok: {rok:yyyy-MM-dd}, ostalo {daysLeft} dana)");
                Console.ResetColor();
                //Slanje zahteva za promenu roka isteka zadatka
                if (prioritet < 1)
                {
                    System.Console.WriteLine("Unesite novi rok u formatu (yyyy-MM-dd)");
                    string newRok = Console.ReadLine() ?? string.Empty;
                    if (!string.IsNullOrEmpty(newRok))
                    {
                        string produzenje = $"PRODUZENJE:{naziv}:{newRok}";
                        udpClientSocket.SendTo(Encoding.UTF8.GetBytes(produzenje), serverEP);
                    }

                }
            }
            else
            {
                System.Console.WriteLine($"{naziv} za {zaposleni} (rok: {rok:yyyy-MM-dd})");
            }
        }
        udpClientSocket.Close();
    }
}