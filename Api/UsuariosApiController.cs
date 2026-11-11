using AstroMissionMVC.Data;
using AstroMissionMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AstroMissionMVC.Api;

// API REST de usuarios. Devuelve JSON, no vistas.
// Reglas:
//  - Nunca devuelve contraseñas.
//  - Todos los métodos (salvo login) exigen sesión iniciada.
//  - Un usuario solo puede modificar o eliminar su propia cuenta.
[ApiController]
[Route("api/usuarios")]
public class UsuariosApiController : ControllerBase
{
    private readonly UsuarioRepository _usuarioRepository;

    public UsuariosApiController(UsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    private int? UsuarioIdSesion => HttpContext.Session.GetInt32("UsuarioId");

    // POST /api/usuarios/login   { "nombreUsuario": "...", "password": "..." }
    // Crea la misma sesión que el login web, para poder probar la API.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel modelo)
    {
        try
        {
            Usuario? usuario = await _usuarioRepository.ValidarCredencialesAsync(
                modelo.NombreUsuario.Trim(), modelo.Password);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);
            return Ok(new { mensaje = "Sesión iniciada.", usuario.Id, usuario.NombreUsuario });
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // POST /api/usuarios/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { mensaje = "Sesión cerrada." });
    }

    // GET /api/usuarios
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        if (UsuarioIdSesion == null) return NoAutenticado();

        try
        {
            List<Usuario> usuarios = await _usuarioRepository.ObtenerTodosAsync();
            // Se devuelve solo id y nombre: la contraseña nunca sale de la base.
            return Ok(usuarios.Select(u => new { u.Id, u.NombreUsuario }));
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // GET /api/usuarios/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        if (UsuarioIdSesion == null) return NoAutenticado();

        try
        {
            Usuario? usuario = await _usuarioRepository.ObtenerPorIdAsync(id);
            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });
            return Ok(new { usuario.Id, usuario.NombreUsuario });
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // POST /api/usuarios   { "nombreUsuario": "...", "password": "..." }
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Usuario usuario)
    {
        if (UsuarioIdSesion == null) return NoAutenticado();

        try
        {
            usuario.Id = await _usuarioRepository.CrearAsync(usuario);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = usuario.Id },
                new { usuario.Id, usuario.NombreUsuario });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "Ese nombre de usuario ya existe." });
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // PUT /api/usuarios/5   (solo tu propio usuario)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] Usuario usuario)
    {
        if (UsuarioIdSesion == null) return NoAutenticado();
        if (UsuarioIdSesion != id) return NoPermitido();

        try
        {
            usuario.Id = id;
            bool actualizado = await _usuarioRepository.ActualizarAsync(usuario);
            if (!actualizado) return NotFound(new { mensaje = "Usuario no encontrado." });

            HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);
            return Ok(new { usuario.Id, usuario.NombreUsuario });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "Ese nombre de usuario ya existe." });
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // DELETE /api/usuarios/5   (solo tu propio usuario y sin misiones)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (UsuarioIdSesion == null) return NoAutenticado();
        if (UsuarioIdSesion != id) return NoPermitido();

        try
        {
            bool eliminado = await _usuarioRepository.EliminarAsync(id);
            if (!eliminado) return NotFound(new { mensaje = "Usuario no encontrado." });

            HttpContext.Session.Clear(); // la cuenta ya no existe
            return NoContent();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            // La llave foránea de misiones impide borrar un usuario con misiones.
            return Conflict(new { mensaje = "No se puede eliminar: el usuario tiene misiones asociadas." });
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // ---- Respuestas comunes ----

    private IActionResult NoAutenticado() =>
        Unauthorized(new { mensaje = "Debes iniciar sesión. Usa POST /api/usuarios/login." });

    private IActionResult NoPermitido() =>
        StatusCode(403, new { mensaje = "Solo puedes modificar o eliminar tu propio usuario." });

    private IActionResult ErrorBaseDatos() =>
        StatusCode(500, new { mensaje = "Error al conectar o consultar PostgreSQL." });
}
