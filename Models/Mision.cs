using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AstroMissionMVC.Models;

// Representa una fila de la tabla "misiones".
public class Mision
{
    // Estados permitidos (los mismos que el CHECK de la tabla).
    public static readonly string[] Estados = { "Planeada", "En progreso", "Completada", "Cancelada" };

    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    [Display(Name = "Ubicación")]
    public string Ubicacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de la misión")]
    public DateTime FechaMision { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression("^(Planeada|En progreso|Completada|Cancelada)$",
        ErrorMessage = "Estado no válido.")]
    public string Estado { get; set; } = "Planeada";

    // Llave foránea hacia usuarios.id.
    // [BindNever]: MVC NO la toma del formulario.
    // [ValidateNever]: no se valida, porque el controlador la asigna desde la sesión.
    [BindNever]
    [ValidateNever]
    public int UsuarioId { get; set; }
}
