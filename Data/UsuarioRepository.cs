using AstroMissionMVC.Models;
using Npgsql;

namespace AstroMissionMVC.Data;

// Todas las consultas SQL de la tabla "usuarios".
// Los valores SIEMPRE van como parámetros (@nombre), nunca concatenados.
public class UsuarioRepository
{
    private readonly ConexionDB _conexionDB;

    public UsuarioRepository(ConexionDB conexionDB)
    {
        _conexionDB = conexionDB;
    }

    // Login: devuelve el usuario si nombre y contraseña coinciden; si no, null.
    public async Task<Usuario?> ValidarCredencialesAsync(string nombreUsuario, string password)
    {
        const string sql = @"SELECT id, nombreusuario
                             FROM usuarios
                             WHERE nombreusuario = @nombreUsuario AND password = @password";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();

        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("nombreUsuario", nombreUsuario);
        comando.Parameters.AddWithValue("password", password);

        await using var lector = await comando.ExecuteReaderAsync();
        if (await lector.ReadAsync())
        {
            return new Usuario
            {
                Id = lector.GetInt32(0),
                NombreUsuario = lector.GetString(1)
            };
        }
        return null;
    }

    // La contraseña nunca se lee en estas consultas.
    public async Task<List<Usuario>> ObtenerTodosAsync()
    {
        const string sql = "SELECT id, nombreusuario FROM usuarios ORDER BY id";
        var usuarios = new List<Usuario>();

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            usuarios.Add(new Usuario { Id = lector.GetInt32(0), NombreUsuario = lector.GetString(1) });
        }
        return usuarios;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        const string sql = "SELECT id, nombreusuario FROM usuarios WHERE id = @id";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("id", id);
        await using var lector = await comando.ExecuteReaderAsync();

        if (await lector.ReadAsync())
        {
            return new Usuario { Id = lector.GetInt32(0), NombreUsuario = lector.GetString(1) };
        }
        return null;
    }

    // Inserta y devuelve el id generado por PostgreSQL (RETURNING id).
    public async Task<int> CrearAsync(Usuario usuario)
    {
        const string sql = @"INSERT INTO usuarios (nombreusuario, password)
                             VALUES (@nombreUsuario, @password)
                             RETURNING id";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("nombreUsuario", usuario.NombreUsuario);
        comando.Parameters.AddWithValue("password", usuario.Password);

        return (int)(await comando.ExecuteScalarAsync())!;
    }

    // Devuelve true si se actualizó alguna fila.
    public async Task<bool> ActualizarAsync(Usuario usuario)
    {
        const string sql = @"UPDATE usuarios
                             SET nombreusuario = @nombreUsuario, password = @password
                             WHERE id = @id";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("nombreUsuario", usuario.NombreUsuario);
        comando.Parameters.AddWithValue("password", usuario.Password);
        comando.Parameters.AddWithValue("id", usuario.Id);

        return await comando.ExecuteNonQueryAsync() > 0;
    }

    // Si el usuario tiene misiones, PostgreSQL lanza un error de llave foránea (23503).
    public async Task<bool> EliminarAsync(int id)
    {
        const string sql = "DELETE FROM usuarios WHERE id = @id";

        await using var conexion = _conexionDB.ObtenerConexion();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("id", id);

        return await comando.ExecuteNonQueryAsync() > 0;
    }
}
