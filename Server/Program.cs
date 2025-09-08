using System.Net;
using System.Net.Sockets;
using System.Text;
using Server;

//Dictionary string Korisnicko ime - Menadzer projekta i listom objekata klase ZadatakProjekta
Dictionary<string, ZadatakProjekta> menadgerAndProjects = new Dictionary<string, ZadatakProjekta>();
//Kreiranje uticnice za prijem datagrama
Socket recvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

//Povezivanje uticnice sa bilo kojom adresom na lokalnom racunaru i portom 27015
IPEndPoint recvEndPoint = new IPEndPoint(IPAddress.Any, 27015);
recvSocket.Bind(recvEndPoint);

//Bafer za prijem podataka
byte[] recvBuffer = new byte[1024];
EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);

try
{
    //Prijem datagrama
    int bytesReceived = recvSocket.ReceiveFrom(recvBuffer, ref senderEndPoint);

    //Konverzija primljenih bajtova u string
    string receivedMessage = Encoding.UTF8.GetString(recvBuffer, 0, bytesReceived);
    System.Console.WriteLine("Received {0} bytes from {1}: {2}", bytesReceived, senderEndPoint, receivedMessage);
}
catch (SocketException ex)
{
    System.Console.WriteLine("recvfrom failed with error {0}", ex.Message);
    throw;
}
finally
{
    recvSocket.Close();
}