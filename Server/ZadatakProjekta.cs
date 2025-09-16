using System;
using System.Net;
using Shared;

namespace Server;

public class ZadatakProjekta
{
    public string Naziv { get; set; } = String.Empty;
    public string Zaposleni { get; set; } = String.Empty;
    public Status Status { get; set; } = Status.NaCekanju;
    public DateTime Rok { get; set; }
    public int Prioritet { get; set; }

    public override string ToString()
    {
        return $"{Naziv}|{Zaposleni}|{Rok:yyyy-MM-dd}|{Prioritet}|{Status}";
    }

    public static ZadatakProjekta FromString(string s)
    {
        var parts = s.Split("|");
        return new ZadatakProjekta
        {
            Naziv = parts[0],
            Zaposleni = parts[1],
            Rok = DateTime.Parse(parts[2]),
            Prioritet = int.Parse(parts[3]),
            Status = Enum.Parse<Status>(parts[4])
        };
    }

}
