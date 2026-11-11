using Npgsql;

namespace AstroMissionMVC.Data;
public class ConexionDB
{
    private readonly string _cadenaConexion;

    public ConexionDB(IConfiguration configuration)
    {
        _cadenaConexion = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'PostgreSQL' en appsettings.json.");
    }

    // devuelve una conexión nueva.
    // quien la usa la abre y la cierra con using para liberarla siempre.
    public NpgsqlConnection ObtenerConexion()
    {
        return new NpgsqlConnection(_cadenaConexion);
    }
}
