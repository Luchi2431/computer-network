using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Server;
using Shared;

class Manadzer
{
    static void Main()
    {
        System.Console.WriteLine("Unesite svoje korisnicko ime:");
        string username = Console.ReadLine() ?? string.Empty;
        string filePath = $"menadzer-{username}.txt";
        if (File.Exists(filePath))
        {
            username = File.ReadAllText(filePath);
            System.Console.WriteLine($"Dobrodosli nazad: {username}!");
        }
        else
        {
            File.WriteAllText(filePath, username);
        }

        int udpPort = 9000;
        //Kreiranje udp socketa za slanje poruke
        Socket udpClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //Podesavanje adrese primaoca
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, udpPort);

        //Kreiranje praznog baffera i priprema EP da u njega upisemo Adresu Servera
        byte[] buffer = new byte[1024];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        string message = $"MENADZER:{username}";
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

            while (true)
            {
                System.Console.WriteLine("\nKliknite 1 ako zelite da unesete novi zadatak");
                System.Console.WriteLine("Kliknite 2 ako zelite pregled svih zadataka");
                System.Console.WriteLine("Kliknite 0 ako zelite da izadjete iz menija\n");
                string option = Console.ReadLine() ?? "0";
                switch (option)
                {
                    case "0":
                        System.Console.WriteLine("TCP Konekcija prekinuta");
                        udpClientSocket.Close();
                        tcpClient.Close();
                        return;
                    case "1":
                        //Kreiranje zadatka
                        System.Console.WriteLine("Naziv zadatka:");
                        string naziv = Console.ReadLine() ?? string.Empty;
                        System.Console.WriteLine("Zaposleni:");
                        string zaposleni = Console.ReadLine() ?? string.Empty;
                        System.Console.WriteLine("Rok yyyy-mm-dd:");
                        string rok = Console.ReadLine() ?? string.Empty;
                        System.Console.WriteLine("Prioritet (int):");
                        string prioritet = Console.ReadLine() ?? string.Empty;

                        string zadatakPoruka = $"{naziv}|{zaposleni}|{rok}|{prioritet}|{Status.NaCekanju}";
                        tcpClient.Send(Encoding.UTF8.GetBytes(zadatakPoruka));
                        System.Console.WriteLine("Zadatak poslat serveru\n");
                        break;
                    case "2":
                        //PREGLED ZADATKA
                        string pregledPoruke = $"PREGLED:{username}";
                        udpClientSocket.SendTo(Encoding.UTF8.GetBytes(pregledPoruke), serverEP);

                        received = udpClientSocket.ReceiveFrom(buffer, ref remoteEP);
                        string tasksStr = Encoding.UTF8.GetString(buffer, 0, received);

                        //Razdeli string na delove i prikazi zadatke
                        string[] lines = tasksStr.Split("\n");
                        System.Console.WriteLine("Svi zadaci ovog Menadzera");
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            try
                            {
                                var zadatak = ZadatakProjekta.FromString(line);


                                //Pregled zadataka sa komentarom
                                if (zadatak.Status == Status.Zavrsen && !string.IsNullOrWhiteSpace(zadatak.Komentar))
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    System.Console.WriteLine($"{zadatak.Naziv} (Zavrsio: {zadatak.Zaposleni}) -> Komentar: {zadatak.Komentar}");
                                    Console.ResetColor();
                                }
                                //Pregled zadataka u toku
                                else if (zadatak.Status == Status.UToku)
                                {
                                    System.Console.WriteLine("\n Pregled zadataka 'U Toku':");
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    System.Console.WriteLine($"Naziv zadatka: {zadatak.Naziv} za {zadatak.Zaposleni} (rok: {zadatak.Rok:yyyy-MM-dd})");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    System.Console.WriteLine(zadatak.ToString());
                                    int daysLeft = (zadatak.Rok - DateTime.Now).Days;
                                    if (daysLeft < 2)
                                    {
                                        Console.BackgroundColor = ConsoleColor.Yellow;
                                        System.Console.WriteLine($"\n!!!{zadatak.Naziv} za {zadatak.Zaposleni} (rok: {zadatak.Rok:yyyy-MM-dd}, ostalo {daysLeft} dana)");
                                        Console.ResetColor();
                                        //Slanje zahteva za promenu roka isteka zadatka
                                        if (zadatak.Prioritet < 1)
                                        {
                                            System.Console.WriteLine("Unesite novi rok u formatu (yyyy-MM-dd)");
                                            string newRok = Console.ReadLine() ?? string.Empty;
                                            if (!string.IsNullOrEmpty(newRok))
                                            {
                                                string produzenje = $"PRODUZENJE:{zadatak.Naziv}:{newRok}";
                                                udpClientSocket.SendTo(Encoding.UTF8.GetBytes(produzenje), serverEP);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                System.Console.WriteLine($"Ne mogu da parsiram zadatak: {line}({ex.Message})");
                            }
                        }
                        break;
                    default:
                        System.Console.WriteLine("Uneli ste pogresku komandu!");
                        break;
                }
            }
        }
    }
}