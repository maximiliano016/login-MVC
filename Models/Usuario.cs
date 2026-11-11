using System.ComponentModel.DataAnnotations;

namespace AstroMissionMVC.Models;

// Representa una fila de la tabla "usuarios".
public class Usuario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
