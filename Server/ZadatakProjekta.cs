using System;
using System.Net;
using Shared;

namespace Server;

public class ZadatakProjekta
{
    public string Naziv { get; set; } = String.Empty;
    public string Zaposleni { get; set; } = String.Empty;
    public Status Status { get; set; }
    public DateTime Rok { get; set; }
    public int Prioritet { get; set; }

}
