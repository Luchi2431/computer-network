using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Server;

class Program
{
    static void Main()
    {
        // UDP deo
        Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, 9000);
        udpSocket.Bind(udpEndPoint);

        // TCP deo
        int tcpPort = 10000;
        Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpListener.Bind(new IPEndPoint(IPAddress.Any, 10000)); // slobodan port
        tcpListener.Listen(10);


        Console.WriteLine($"UDP server na portu 9000, TCP server na portu {tcpPort}");

        // Za praćenje TCP klijenata
        List<Socket> tcpClients = new List<Socket>();

        // Mapiranje korisnika -> zadaci
        Dictionary<string, List<ZadatakProjekta>> zadaci = new Dictionary<string, List<ZadatakProjekta>>();

        //Mapiranje zaposlenih 
        Dictionary<string, Socket> employeesConnections = new Dictionary<string, Socket>();
        Queue<string> pendingEmployees = new Queue<string>();

        // Mapiranje TCP socket -> korisnickoIme
        Dictionary<Socket, string> tcpClientUser = new Dictionary<Socket, string>();

        // Privremeno čuvamo korisnika koji je poslao MENADZER: preko UDP-a,
        // a tek se posle povezuje na TCP
        Queue<string> pendingUsers = new Queue<string>();

        byte[] recvBuffer = new byte[1024];

        while (true)
        {
            // Polling lista
            List<Socket> checkRead = new List<Socket>();
            checkRead.Add(udpSocket);
            checkRead.Add(tcpListener);
            checkRead.AddRange(tcpClients);
            try
            {
                Socket.Select(checkRead, null, null, 1000000);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // samo nastavi sa radom
                    continue;
                }
                else
                {
                    Console.WriteLine($"[GRESKA] {ex.Message}");
                    continue;
                }
            }


            foreach (Socket sock in checkRead)
            {
                // --- UDP deo ---
                if (sock == udpSocket)
                {
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int received = udpSocket.ReceiveFrom(recvBuffer, ref remoteEP);
                    string message = Encoding.UTF8.GetString(recvBuffer, 0, received);

                    if (message.StartsWith("MENADZER:"))
                    {
                        string korisnickoIme = message.Split(':')[1];
                        Console.WriteLine($"Novi menadzer: {korisnickoIme}");

                        if (!zadaci.ContainsKey(korisnickoIme))
                            zadaci[korisnickoIme] = new List<ZadatakProjekta>();

                        // upisujemo korisnika u pendingUsers – on se tek treba povezati na TCP
                        pendingUsers.Enqueue(korisnickoIme);

                        // odgovaramo koji je TCP port
                        string odgovor = $"TCP_PORT:{tcpPort}";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(odgovor), remoteEP);
                    }
                    else if (message.StartsWith("PREGLED:"))
                    {
                        string korisnickoIme = message.Split(':')[1];

                        if (!zadaci.ContainsKey(korisnickoIme) || zadaci[korisnickoIme].Count == 0)
                        {
                            string odgovor = "Nema zadataka u toku";
                            udpSocket.SendTo(Encoding.UTF8.GetBytes(odgovor), remoteEP);
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var zad in zadaci[korisnickoIme])
                            {
                                sb.AppendLine(zad.ToString());
                                // svaki zadatak si već čuvao kao string "naziv|zaposleni|rok|prioritet"
                            }
                            udpSocket.SendTo(Encoding.UTF8.GetBytes(sb.ToString()), remoteEP);
                        }
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
                    else if (message.StartsWith("ZAPOSLENI:"))
                    {
                        string imeZaposlenog = message.Split(":")[1];
                        System.Console.WriteLine($"Novi zaposleni: {imeZaposlenog}");

                        pendingEmployees.Enqueue(imeZaposlenog);

                        //Odgovor zaposlenom na koji tcp port moze da se nakaci
                        string response = $"TCP_PORT:{tcpPort}";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(response), remoteEP);
                    }
                }
                // --- TCP Accept ---
                else if (sock == tcpListener)
                {
                    Socket client = tcpListener.Accept();
                    tcpClients.Add(client);

                    // uparimo ga sa korisnikom koji je poslednji poslao MENADZER preko UDP-a
                    if (pendingUsers.Count > 0)
                    {
                        string korisnickoIme = pendingUsers.Dequeue();
                        tcpClientUser[client] = korisnickoIme;
                        Console.WriteLine($"TCP menadzer povezan: {korisnickoIme}");
                    }
                    else if (pendingEmployees.Count > 0)
                    {
                        string imeZaposlenog = pendingEmployees.Dequeue();
                        employeesConnections[imeZaposlenog] = client;
                        Console.WriteLine($"TCP zaposleni povezan: {imeZaposlenog}");
                    }
                    else
                    {
                        System.Console.WriteLine("TCP klijent povezan, ali nema pending korisnika");
                    }
                }
                // --- TCP poruke ---
                else
                {
                    int received = 0;
                    try
                    {
                        received = sock.Receive(recvBuffer);
                    }
                    catch (SocketException)
                    {
                        received = 0;
                    }

                    if (received == 0)
                    {
                        Console.WriteLine("TCP klijent se odjavio.");
                        tcpClients.Remove(sock);
                        sock.Close();
                        continue;
                    }

                    if (received != 0)
                    {
                        string zadatakPoruka = Encoding.UTF8.GetString(recvBuffer, 0, received);
                        Console.WriteLine($"Primljen zadatak: {zadatakPoruka}");
                        ZadatakProjekta zadatak = ZadatakProjekta.FromString(zadatakPoruka);

                        if (tcpClientUser.ContainsKey(sock))
                        {
                            string korisnickoIme = tcpClientUser[sock];
                            zadaci[korisnickoIme].Add(zadatak);
                            Console.WriteLine($"Zadatak dodat za {korisnickoIme}");
                        }
                        else
                        {
                            Console.WriteLine("Primljen zadatak od nepoznatog klijenta!");
                        }
                    }
                    else
                    {
                        // klijent se diskonektovao
                        Console.WriteLine("TCP klijent se odjavio.");
                        tcpClients.Remove(sock);
                        if (tcpClientUser.ContainsKey(sock))
                            tcpClientUser.Remove(sock);
                        sock.Close();
                    }
                }
            }
        }
    }
}
