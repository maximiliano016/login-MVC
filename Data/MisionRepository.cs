using AstroMissionMVC.Models;
using Npgsql;
using NpgsqlTypes;

namespace AstroMissionMVC.Data;

// Todas las consultas SQL de la tabla "misiones".
// REGLA DE SEGURIDAD: todas las consultas filtran por usuarioid,
// así un usuario nunca puede leer, editar ni borrar misiones de otro.
public class MisionRepository
{
    private readonly ConexionDB _conexionDB;

    public MisionRepository(ConexionDB conexionDB)
    {
        _conexionDB = conexionDB;
    }

    // Solo las misiones del usuario indicado.
    public async Task<List<Mision>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        const string sql = @"SELECT id, nombre, descripcion, ubicacion, fechamision, estado, usuarioid
                             FROM misiones
                             WHERE usuarioid = @usuarioId
                             ORDER BY fechamision";
        var misiones = new List<Mision>();

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("usuarioId", usuarioId);
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            misiones.Add(LeerMision(lector));
        }
        return misiones;
    }

    // Devuelve la misión SOLO si pertenece al usuario; si no, null.
    public async Task<Mision?> ObtenerPorIdAsync(int id, int usuarioId)
    {
        const string sql = @"SELECT id, nombre, descripcion, ubicacion, fechamision, estado, usuarioid
                             FROM misiones
                             WHERE id = @id AND usuarioid = @usuarioId";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("id", id);
        comando.Parameters.AddWithValue("usuarioId", usuarioId);
        await using var lector = await comando.ExecuteReaderAsync();

        return await lector.ReadAsync() ? LeerMision(lector) : null;
    }

    // Inserta la misión con el UsuarioId que puso el controlador (de la sesión).
    public async Task<int> CrearAsync(Mision mision)
    {
        const string sql = @"INSERT INTO misiones (nombre, descripcion, ubicacion, fechamision, estado, usuarioid)
                             VALUES (@nombre, @descripcion, @ubicacion, @fechaMision, @estado, @usuarioId)
                             RETURNING id";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarParametros(comando, mision);

        return (int)(await comando.ExecuteScalarAsync())!;
    }

    // El WHERE incluye usuarioid: si la misión es de otro usuario, no se actualiza nada.
    public async Task<bool> ActualizarAsync(Mision mision)
    {
        const string sql = @"UPDATE misiones
                             SET nombre = @nombre, descripcion = @descripcion, ubicacion = @ubicacion,
                                 fechamision = @fechaMision, estado = @estado
                             WHERE id = @id AND usuarioid = @usuarioId";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        AgregarParametros(comando, mision);
        comando.Parameters.AddWithValue("id", mision.Id);

        return await comando.ExecuteNonQueryAsync() > 0;
    }

    // Igual que en Actualizar: solo borra si la misión es del usuario.
    public async Task<bool> EliminarAsync(int id, int usuarioId)
    {
        const string sql = "DELETE FROM misiones WHERE id = @id AND usuarioid = @usuarioId";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("id", id);
        comando.Parameters.AddWithValue("usuarioId", usuarioId);

        return await comando.ExecuteNonQueryAsync() > 0;
    }

    // ---- Métodos de ayuda ----

    // Convierte la fila actual del lector en un objeto Mision.
    private static Mision LeerMision(NpgsqlDataReader lector)
    {
        return new Mision
        {
            Id = lector.GetInt32(0),
            Nombre = lector.GetString(1),
            Descripcion = lector.GetString(2),
            Ubicacion = lector.GetString(3),
            FechaMision = lector.GetDateTime(4),
            Estado = lector.GetString(5),
            UsuarioId = lector.GetInt32(6)
        };
    }

    // Parámetros comunes de INSERT y UPDATE.
    private static void AgregarParametros(NpgsqlCommand comando, Mision mision)
    {
        comando.Parameters.AddWithValue("nombre", mision.Nombre);
        comando.Parameters.AddWithValue("descripcion", mision.Descripcion);
        comando.Parameters.AddWithValue("ubicacion", mision.Ubicacion);
        // Se indica el tipo DATE porque la columna no guarda hora.
        comando.Parameters.AddWithValue("fechaMision", NpgsqlDbType.Date, mision.FechaMision.Date);
        comando.Parameters.AddWithValue("estado", mision.Estado);
        comando.Parameters.AddWithValue("usuarioId", mision.UsuarioId);
    }
}
