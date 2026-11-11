using AstroMissionMVC.Data;
using AstroMissionMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace AstroMissionMVC.Controllers;

// Controlador MVC del CRUD de misiones
public class MisionesController : Controller
{
    private const string ErrorBaseDatos =
        "Ocurrió un problema con la base de datos. Verifica que PostgreSQL esté encendido.";

    private readonly MisionRepository _misionRepository;

    public MisionesController(MisionRepository misionRepository)
    {
        _misionRepository = misionRepository;
    }

    // Id del usuario autenticado, no lo toma del formulario
    private int UsuarioIdSesion => HttpContext.Session.GetInt32("UsuarioId") ?? 0;

    // Se ejecuta ANTES de cualquier acción de este controlador.
    // Si no hay sesión, redirige al login. Así no se repite la validación en cada método.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetInt32("UsuarioId") == null)
        {
            TempData["Error"] = "Debes iniciar sesión para ver tus misiones.";
            context.Result = RedirectToAction("Index", "Login");
            return;
        }
        base.OnActionExecuting(context);
    }

    // get misiones. lista solo las misiones del usuario de la sesión
    public async Task<IActionResult> Index()
    {
        try
        {
            List<Mision> misiones = await _misionRepository.ObtenerPorUsuarioAsync(UsuarioIdSesion);
            return View(misiones);
        }
        catch (NpgsqlException)
        {
            ViewBag.Error = ErrorBaseDatos;
            return View(new List<Mision>());
        }
    }

    // GET /Misiones/Details/5
    public Task<IActionResult> Details(int id) => MostrarMisionPropia(id);

    // GET /Misiones/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Mision());
    }

    // POST /Misiones/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Mision mision)
    {
        if (!ModelState.IsValid)
        {
            return View(mision);
        }

        try
        {
            // El dueño de la misión es el usuario de la sesión.
            mision.UsuarioId = UsuarioIdSesion;
            await _misionRepository.CrearAsync(mision);
            TempData["Mensaje"] = "Misión creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (NpgsqlException)
        {
            ViewBag.Error = ErrorBaseDatos;
            return View(mision);
        }
    }

    // GET /Misiones/Edit/5
    [HttpGet]
    public Task<IActionResult> Edit(int id) => MostrarMisionPropia(id);

    // POST /Misiones/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Mision mision)
    {
        mision.Id = id;
        mision.UsuarioId = UsuarioIdSesion;

        if (!ModelState.IsValid)
        {
            return View(mision);
        }

        try
        {
            // El UPDATE filtra por id Y usuarioid: si no es suya, no cambia nada.
            bool actualizada = await _misionRepository.ActualizarAsync(mision);
            if (!actualizada)
            {
                TempData["Error"] = "La misión no existe o no pertenece a tu usuario.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Mensaje"] = "Misión actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (NpgsqlException)
        {
            ViewBag.Error = ErrorBaseDatos;
            return View(mision);
        }
    }

    // GET /Misiones/Delete/5  -> pantalla de confirmación
    [HttpGet]
    public Task<IActionResult> Delete(int id) => MostrarMisionPropia(id);

    // POST /Misiones/Delete/5  -> elimina después de confirmar
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // El DELETE filtra por id Y usuarioid.
            bool eliminada = await _misionRepository.EliminarAsync(id, UsuarioIdSesion);
            TempData[eliminada ? "Mensaje" : "Error"] = eliminada
                ? "Misión eliminada correctamente."
                : "La misión no existe o no pertenece a tu usuario.";
        }
        catch (NpgsqlException)
        {
            TempData["Error"] = ErrorBaseDatos;
        }
        return RedirectToAction(nameof(Index));
    }

    // Usado por Details, Edit (GET) y Delete (GET):
    // busca la misión por id y por el usuario de la sesión.
    // Si es de otro usuario (o no existe), vuelve al listado con un mensaje.
    // View(mision) usa la vista con el nombre de la acción actual.
    private async Task<IActionResult> MostrarMisionPropia(int id)
    {
        try
        {
            Mision? mision = await _misionRepository.ObtenerPorIdAsync(id, UsuarioIdSesion);
            if (mision == null)
            {
                TempData["Error"] = "La misión no existe o no pertenece a tu usuario.";
                return RedirectToAction(nameof(Index));
            }
            return View(mision);
        }
        catch (NpgsqlException)
        {
            TempData["Error"] = ErrorBaseDatos;
            return RedirectToAction(nameof(Index));
        }
    }
}
