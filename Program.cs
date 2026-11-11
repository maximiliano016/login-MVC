using AstroMissionMVC.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// 1. SERVICIOS
// ---------------------------------------------------------------

// MVC: controladores + vistas Razor (.cshtml).
// También habilita los controladores de la carpeta Api.
builder.Services.AddControllersWithViews();

// Acceso a PostgreSQL con Npgsql.
// ConexionDB lee la cadena "PostgreSQL" de appsettings.json una sola vez.
builder.Services.AddSingleton<ConexionDB>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<MisionRepository>();

// Sesiones HTTP: los datos se guardan en memoria del servidor
// y el navegador solo guarda una cookie con el identificador de sesión.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".AstroMission.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30); // se cierra tras 30 min sin actividad
    options.Cookie.HttpOnly = true;                 // JavaScript no puede leer la cookie
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ---------------------------------------------------------------
// 2. PIPELINE (el orden importa)
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// La sesión debe activarse ANTES de mapear los controladores.
app.UseSession();

// Archivos estáticos de wwwroot (css, js, images, lib).
app.MapStaticAssets();

// Ruteo por atributos: controladores de la carpeta Api ([Route("api/...")]).
app.MapControllers();

// Ruteo convencional MVC: /Controlador/Accion/id
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
