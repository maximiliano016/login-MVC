using AstroMissionMVC.Data;
using AstroMissionMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AstroMissionMVC.Api;

// API REST de misiones. Devuelve JSON, no vistas.
// Usa la MISMA sesión que el sitio MVC: solo trabaja con las misiones
// del usuario que inició sesión (POST /api/usuarios/login o el login web).
[ApiController]
[Route("api/misiones")]
public class MisionesApiController : ControllerBase
{
    private readonly MisionRepository _misionRepository;

    public MisionesApiController(MisionRepository misionRepository)
    {
        _misionRepository = misionRepository;
    }

    // null si no hay sesión.
    private int? UsuarioIdSesion => HttpContext.Session.GetInt32("UsuarioId");

    // GET /api/misiones
    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        int? usuarioId = UsuarioIdSesion;
        if (usuarioId == null) return NoAutenticado();

        try
        {
            return Ok(await _misionRepository.ObtenerPorUsuarioAsync(usuarioId.Value));
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // GET /api/misiones/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        int? usuarioId = UsuarioIdSesion;
        if (usuarioId == null) return NoAutenticado();

        try
        {
            Mision? mision = await _misionRepository.ObtenerPorIdAsync(id, usuarioId.Value);
            return mision == null ? NoEncontrada() : Ok(mision);
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // POST /api/misiones
    // [ApiController] valida el modelo automáticamente y responde 400 si hay errores.
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Mision mision)
    {
        int? usuarioId = UsuarioIdSesion;
        if (usuarioId == null) return NoAutenticado();

        try
        {
            // Se ignora cualquier usuarioId enviado en el JSON.
            mision.UsuarioId = usuarioId.Value;
            mision.Id = await _misionRepository.CrearAsync(mision);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = mision.Id }, mision);
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // PUT /api/misiones/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] Mision mision)
    {
        int? usuarioId = UsuarioIdSesion;
        if (usuarioId == null) return NoAutenticado();

        try
        {
            mision.Id = id;
            mision.UsuarioId = usuarioId.Value;
            bool actualizada = await _misionRepository.ActualizarAsync(mision);
            return actualizada ? Ok(mision) : NoEncontrada();
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // DELETE /api/misiones/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        int? usuarioId = UsuarioIdSesion;
        if (usuarioId == null) return NoAutenticado();

        try
        {
            bool eliminada = await _misionRepository.EliminarAsync(id, usuarioId.Value);
            return eliminada ? NoContent() : NoEncontrada();
        }
        catch (NpgsqlException)
        {
            return ErrorBaseDatos();
        }
    }

    // ---- Respuestas comunes ----

    private IActionResult NoAutenticado() =>
        Unauthorized(new { mensaje = "Debes iniciar sesión. Usa POST /api/usuarios/login." });

    private IActionResult NoEncontrada() =>
        NotFound(new { mensaje = "La misión no existe o no pertenece a tu usuario." });

    private IActionResult ErrorBaseDatos() =>
        StatusCode(500, new { mensaje = "Error al conectar o consultar PostgreSQL." });
}
