using System.Net;
using System.Net.Sockets;
using System.Text;
using Server;



class KolaborativniServis
{
    static Dictionary<string, List<ZadatakProjekta>> zadaci = new Dictionary<string, List<ZadatakProjekta>>();

    static void Main()
    {
        int udpPort = 9000;
        int tcpPort = 10000;

        //Kreiranje udp Socketa
        Socket udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //Povezivanje uticnice sa bilo kojom adresom na lokalnom racunaru i portom 27015
        IPEndPoint localUdpEndPoint = new IPEndPoint(IPAddress.Any, udpPort);
        udpServer.Bind(localUdpEndPoint);
        System.Console.WriteLine($"UDP Server pokrenut na portu {udpPort}...");

        // TCP socket za prijem zadataka
        Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint localTcpEndPoint = new IPEndPoint(IPAddress.Any, tcpPort);
        tcpListener.Bind(localTcpEndPoint);
        tcpListener.Listen(5);
        System.Console.WriteLine($"TCP Server pokrenut na portu {tcpPort}");

        //bafer za prijem podataka
        byte[] recvBuffer = new byte[1024];
        EndPoint remoteUdpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                int received = udpServer.ReceiveFrom(recvBuffer, ref remoteUdpEndPoint);
                string message = Encoding.UTF8.GetString(recvBuffer, 0, received);

                System.Console.WriteLine($"Primljena poruka: {message}");

                if (message.StartsWith("MENADZER:"))
                {
                    string korisnickoIme = message.Split(':')[1];
                    if (!zadaci.ContainsKey(korisnickoIme))
                    {
                        zadaci[korisnickoIme] = new List<ZadatakProjekta>();
                    }

                    string odgovor = $"TCP_PORT:{tcpPort}";
                    byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovor);
                    udpServer.SendTo(odgovorBytes, remoteUdpEndPoint);
                    System.Console.WriteLine($"Poslat TCP PORT Korisniku {korisnickoIme}");

                    //Server dobija klijentsku uticnicu
                    Socket client = tcpListener.Accept();
                    System.Console.WriteLine("Menadzer povezan preko TCP!");

                    //Primanje zadataka
                    received = client.Receive(recvBuffer);
                    string zadatakPoruka = Encoding.UTF8.GetString(recvBuffer, 0, received);
                    System.Console.WriteLine($"Primljeno preko TCP: {zadatakPoruka}");

                    //Parsiranje poruke i dodavanje u Dictionary
                    string[] delovi = zadatakPoruka.Split('|');
                    if (delovi.Length >= 4)
                    {
                        ZadatakProjekta zadatak = new ZadatakProjekta
                        {
                            Naziv = delovi[0],
                            Zaposleni = delovi[1],
                            Rok = DateTime.Parse(delovi[2]),
                            Prioritet = int.Parse(delovi[3])
                        };
                        zadaci[korisnickoIme].Add(zadatak);
                        System.Console.WriteLine($"Zadatak dodat: {zadatak.Naziv} za {zadatak.Zaposleni}");
                    }
                    client.Close();
                }
                else if (message.StartsWith("PREGLED:"))
                {
                    //Menadzer trazi pregled zadataka
                    string korisnickoIme = message.Split(':')[1];
                    if (!zadaci.ContainsKey(korisnickoIme) || zadaci[korisnickoIme].Count == 0)
                    {
                        string response = "Nema zadataka u toku";
                        udpServer.SendTo(Encoding.UTF8.GetBytes(response), remoteUdpEndPoint);
                    }
                    //Kreiramo string sa svim zadacima u toku
                    StringBuilder sb = new StringBuilder();
                    foreach (var zad in zadaci[korisnickoIme])
                    {
                        if (zad.Status == Shared.Status.UToku)
                        {
                            sb.AppendLine($"{zad.Naziv}|{zad.Zaposleni}|{zad.Rok:yyyy-MM-dd}|{zad.Prioritet}");
                            System.Console.WriteLine(sb.ToString());
                        }
                    }
                    udpServer.SendTo(Encoding.UTF8.GetBytes(sb.ToString()), remoteUdpEndPoint);
                }
                else if (message.StartsWith("PRODUZENJE:"))
                {
                    string[] parts = message.Split(":");
                    if (parts.Length == 3)
                    {
                        //Podeli string na delove i upisi rok u taj zadatak koji hoces da produzis
                        string nazivZadatka = parts[1];
                        DateTime newRok = DateTime.Parse(parts[2]);
                        foreach (var pair in zadaci)
                        {
                            var zad = pair.Value.FirstOrDefault(z => z.Naziv == nazivZadatka);
                            if (zad != null)
                            {
                                zad.Rok = newRok;
                                System.Console.WriteLine($"Rok zadatka {nazivZadatka} produzen na {newRok:yyyy-MM-dd}");
                            }

                        }
                    }
                }
            }
        }
        finally
        {
            udpServer.Close();
            tcpListener.Close();
        }
    }
}


