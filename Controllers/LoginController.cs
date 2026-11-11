using AstroMissionMVC.Data;
using AstroMissionMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AstroMissionMVC.Controllers;

// controlador MVC del login
// valida las credenciales contra PostgreSQL y maneja la sesión.
public class LoginController : Controller
{
    private readonly UsuarioRepository _usuarioRepository;

    public LoginController(UsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    // get login. muestra el formulario
    [HttpGet]
    public IActionResult Index()
    {
        // Si ya hay sesion, no tiene sentido volver a pedir el login.
        if (HttpContext.Session.GetInt32("UsuarioId") != null)
        {
            return RedirectToAction("Index", "Misiones");
        }
        return View(new LoginViewModel());
    }

    // post login. recibe usuario y contraseña
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel modelo)
    {
        // validar que los campos no esten vacios.
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            // buscar el usuario en PostgreSQL.
            Usuario? usuario = await _usuarioRepository.ValidarCredencialesAsync(
                modelo.NombreUsuario.Trim(), modelo.Password);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View(modelo);
            }

            // credenciales correctas y guarda los datos en la sesion.
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);

            // ir a las misiones del usuario.
            return RedirectToAction("Index", "Misiones");
        }
        catch (NpgsqlException)
        {
            ViewBag.Error = "No se pudo conectar con la base de datos. Verifica que PostgreSQL esté encendido.";
            return View(modelo);
        }
    }

    // post login. cierra la sesión
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Mensaje"] = "Sesión cerrada correctamente.";
        return RedirectToAction("Index");
    }
}
