using System.Net;
using System.Net.Sockets;
using System.Text;
using Server;

class Zaposleni
{
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
            try
            {
                tcpClient.Connect(IPAddress.Loopback, tcpPort);
                System.Console.WriteLine("Zaposleni povezan preko TCP-a sa serverom\n");

                //buffer za prijem poruka
                byte[] buffer = new byte[1024];
                while (true)
                {
                    try
                    {
                        System.Console.WriteLine("Ukucajte 1 ako zelite pregled svih zadataka");
                        System.Console.WriteLine("Ukucajte 2 ako zelis da kompletiras neki zadatak");
                        string option = Console.ReadLine() ?? "0";

                        if (option == "1")
                        {
                            string requestMessageForTasks = $"GET_TASKS:{username}";
                            tcpClient.Send(Encoding.UTF8.GetBytes(requestMessageForTasks));

                            //Prijem poruke sa zadacima
                            int receiver = tcpClient.Receive(buffer);
                            if (receiver == 0)
                            {
                                System.Console.WriteLine("Server je prekinuo vezu");
                                break;
                            }
                            string message = Encoding.UTF8.GetString(buffer, 0, receiver);
                            if (message.StartsWith("TASKS:"))
                            {
                                //Obrada primljenih zadataka za prikaz
                                string tasksData = message.Substring(6);
                                if (tasksData == "NONE")
                                {
                                    System.Console.WriteLine("\n Nemate dodeljenih nezavrsenih zadataka!");
                                }
                                else
                                {
                                    System.Console.WriteLine("\n Vasi zadaci (sortirani po prioritetu):");
                                    System.Console.WriteLine("---------------------------------------");
                                    var tasks = tasksData.Split(";").Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => ZadatakProjekta.FromString(t));
                                    foreach (var task in tasks)
                                    {
                                        System.Console.WriteLine($"Naziv: {task.Naziv}");
                                        System.Console.WriteLine($"Rok: {task.Rok:yyyy-MM-dd}");
                                        System.Console.WriteLine($"Prioritet: {task.Prioritet}");
                                        System.Console.WriteLine($"Status: {task.Status}");
                                        System.Console.WriteLine("---------------------------------------");
                                    }
                                }
                                System.Console.WriteLine("\n Pritisnite ENTER za povratak na meni");
                                Console.ReadLine();
                            }
                        }
                        else if (option == "2")
                        {
                            System.Console.WriteLine("Unesite ime zadatka koji zelite da zapocnete");
                            string taskName = Console.ReadLine() ?? string.Empty;

                            //Prvo postavimo zadatak u stanje "u toku"
                            string startRequest = $"POCNI_ZADATAK:{taskName}";
                            tcpClient.Send(Encoding.UTF8.GetBytes(startRequest));

                            //Cekamo portvdu
                            received = tcpClient.Receive(buffer);
                            response = Encoding.UTF8.GetString(buffer, 0, received);

                            if (response == "STATUS:OK")
                            {
                                System.Console.WriteLine("Zadatak je zapocet. Da li zelite da ga oznacite kao zavrsen? (da/ne)");
                                string choice = Console.ReadLine()?.ToLower() ?? "ne";
                                if (choice == "da")
                                {
                                    string completeRequest = taskName + ":Zavrsen";
                                    System.Console.WriteLine("Da li zelite da dodate komentar uz zadatak? (da/ne)");
                                    string commentChoice = Console.ReadLine()?.ToLower() ?? string.Empty;
                                    if (commentChoice == "da")
                                    {
                                        System.Console.WriteLine("Unesite komentar za zadatak");
                                        string comment = Console.ReadLine() ?? string.Empty;
                                        completeRequest += $"|{comment}";
                                    }

                                    tcpClient.Send(Encoding.UTF8.GetBytes(completeRequest));

                                    //Cekamo potvrdu
                                    received = tcpClient.Receive(buffer);
                                    response = Encoding.UTF8.GetString(buffer, 0, received);

                                    if (response == "STATUS:OK")
                                    {
                                        System.Console.WriteLine("Zadatak je uspesno izvrsen\n");
                                    }
                                }
                            }
                            else
                            {
                                System.Console.WriteLine("Greska pri obradi zadatka.");
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        System.Console.WriteLine($"Greska pri komunikaciji: {ex.Message}");
                        break;
                    }
                }
            }
            catch (SocketException ex)
            {
                System.Console.WriteLine($"Greska pri povezivanju: {ex.Message}");
            }
            finally
            {
                tcpClient.Close();
            }
        }
    }

    static string GetUsername()
    {
        System.Console.WriteLine("Unesite vase korisnickog ime:");
        string username = Console.ReadLine() ?? string.Empty;
        string filePath = $"Zaposleni-{username}.txt";
        if (File.Exists(filePath))
        {
            username = File.ReadAllText(filePath);
            System.Console.WriteLine($"Dobrodosli nazad: {username}");
            return username;
        }
        File.WriteAllText(filePath, username);
        return username;
    }
}