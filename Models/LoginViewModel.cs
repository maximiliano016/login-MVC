using System.ComponentModel.DataAnnotations;

namespace AstroMissionMVC.Models;

// Datos que llegan desde el formulario de login (no es una tabla).
public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu nombre de usuario.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}
