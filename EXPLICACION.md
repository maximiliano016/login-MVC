# AstroMissionMVC — Explicación del proyecto

Plataforma MVC para administrar misiones de observación astronómica.
Cada usuario inicia sesión y solo ve, crea, edita y elimina **sus propias** misiones.

Tecnologías: C#, ASP.NET Core MVC (.NET 10), Razor (.cshtml), PostgreSQL 17, Npgsql 10.0.3,
sesiones HTTP de ASP.NET Core. Diseño propio y minimalista con un solo archivo CSS (`wwwroot/css/site.css`),
sin Bootstrap, sin iconos ni imágenes. jQuery Validation (de la plantilla) valida los formularios en el navegador.

---

## 1. Estructura final de carpetas

```
api-login-prueba/                      (carpeta del proyecto AstroMissionMVC)
├── Api/                               ← API REST (devuelve JSON)
│   ├── UsuariosApiController.cs         /api/usuarios
│   └── MisionesApiController.cs         /api/misiones
├── Controllers/                       ← Controladores MVC (devuelven vistas)
│   ├── HomeController.cs                portada, privacidad, error
│   ├── LoginController.cs               login y logout
│   └── MisionesController.cs            CRUD con vistas
├── Data/                              ← Acceso a PostgreSQL con Npgsql
│   ├── ConexionDB.cs                    lee la cadena "PostgreSQL" y crea conexiones
│   ├── UsuarioRepository.cs             SQL de la tabla usuarios
│   └── MisionRepository.cs              SQL de la tabla misiones
├── Database/                          ← Scripts SQL
│   ├── 01_crear_base.sql
│   └── 02_tablas_y_datos.sql
├── Models/
│   ├── ErrorViewModel.cs
│   ├── Usuario.cs
│   ├── Mision.cs
│   └── LoginViewModel.cs
├── Views/
│   ├── Home/      Index.cshtml, Privacy.cshtml
│   ├── Login/     Index.cshtml
│   ├── Misiones/  Index, Details, Create, Edit, Delete (.cshtml)
│   ├── Shared/    _Layout.cshtml, _Layout.cshtml.css, _ValidationScriptsPartial.cshtml, Error.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/
│   ├── css/site.css         todos los estilos (simples)
│   ├── js/site.js
│   ├── images/              (vacía, el diseño no usa imágenes)
│   └── lib/                 jquery y jquery-validation (de la plantilla)
├── Properties/launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── AstroMissionMVC.csproj
```

Archivos añadidos además de la estructura pedida:
- `Data/` → centraliza el acceso a PostgreSQL para no repetir SQL ni la cadena de conexión.
- `Database/` → scripts SQL para recrear la base.

Diseño: cabecera con el nombre y los enlaces, tabla de misiones y formularios en una caja blanca.
El estado se muestra con un punto de color usando la clase `estado-planeada`, `estado-en-progreso`,
`estado-completada` o `estado-cancelada`, que se arma en la vista con
`estado-@(mision.Estado.ToLower().Replace(" ", "-"))`.

---

## 2. Tablas creadas

Base de datos: **`AstroMissionMVC`** (creada con comillas para respetar las mayúsculas y coincidir
con `Database=AstroMissionMVC` de la cadena de conexión).
Tablas y columnas sin comillas → PostgreSQL las guarda en minúsculas.

**usuarios**

| Columna        | Tipo          | Restricción              |
|----------------|---------------|--------------------------|
| id             | SERIAL        | PRIMARY KEY              |
| nombreusuario  | VARCHAR(50)   | NOT NULL, UNIQUE         |
| password       | VARCHAR(100)  | NOT NULL                 |

**misiones**

| Columna      | Tipo          | Restricción                                              |
|--------------|---------------|----------------------------------------------------------|
| id           | SERIAL        | PRIMARY KEY                                              |
| nombre       | VARCHAR(100)  | NOT NULL                                                 |
| descripcion  | VARCHAR(500)  | NOT NULL                                                 |
| ubicacion    | VARCHAR(100)  | NOT NULL                                                 |
| fechamision  | DATE          | NOT NULL                                                 |
| estado       | VARCHAR(20)   | NOT NULL, CHECK (Planeada, En progreso, Completada, Cancelada) |
| usuarioid    | INT           | NOT NULL, FOREIGN KEY → usuarios(id)                     |

---

## 3. Relación entre tablas

```
usuarios (1) ──────────< (N) misiones
   id   ◄──────────────────  usuarioid   (fk_misiones_usuarios)
```

- Un usuario puede tener muchas misiones.
- Una misión pertenece a un solo usuario (`usuarioid` es obligatorio).
- La llave foránea impide crear misiones de un usuario inexistente y
  borrar un usuario que todavía tiene misiones.

En C#: `Mision.UsuarioId` representa la llave foránea.

---

## 4. Usuarios de prueba

| Usuario      | Contraseña  | Misiones (id)                                             |
|--------------|-------------|-----------------------------------------------------------|
| astronomo1   | astro123    | 1 Perseidas · 2 Saturno · 3 Supernova                     |
| explorador2  | espacio456  | 4 Cielo profundo · 5 Aurora polar · 6 Eclipse lunar       |

Hay un estado de cada tipo, así se ven todos los indicadores de color.

---

## 5. Rutas MVC (vistas)

| Método | Ruta                       | Qué hace                                  |
|--------|----------------------------|-------------------------------------------|
| GET    | `/`                        | Portada (Home/Index)                      |
| GET    | `/Home/Privacy`            | Página de privacidad                      |
| GET    | `/Login`                   | Formulario de login                       |
| POST   | `/Login`                   | Valida credenciales y crea la sesión      |
| POST   | `/Login/Logout`            | Cierra la sesión                          |
| GET    | `/Misiones`                | Lista las misiones del usuario en sesión  |
| GET    | `/Misiones/Details/{id}`   | Detalle (solo si es propia)               |
| GET    | `/Misiones/Create`         | Formulario de nueva misión                |
| POST   | `/Misiones/Create`         | Guarda la misión con el UsuarioId de la sesión |
| GET    | `/Misiones/Edit/{id}`      | Formulario de edición (solo si es propia) |
| POST   | `/Misiones/Edit/{id}`      | Actualiza (solo si es propia)             |
| GET    | `/Misiones/Delete/{id}`    | Pantalla de confirmación (solo si es propia) |
| POST   | `/Misiones/Delete/{id}`    | Elimina (solo si es propia)               |

---

## 6. Rutas de la API (JSON)

Todas usan la **misma sesión** que el sitio web. Para probarlas (más adelante, en Postman)
primero se llama a `POST /api/usuarios/login`; Postman guarda la cookie de sesión.

**/api/usuarios**

| Método | Ruta                     | Respuesta |
|--------|--------------------------|-----------|
| POST   | `/api/usuarios/login`    | 200 inicia sesión · 401 credenciales incorrectas |
| POST   | `/api/usuarios/logout`   | 200 cierra sesión |
| GET    | `/api/usuarios`          | 200 lista (id y nombre, **nunca** la contraseña) |
| GET    | `/api/usuarios/{id}`     | 200 · 404 |
| POST   | `/api/usuarios`          | 201 creado · 409 nombre repetido |
| PUT    | `/api/usuarios/{id}`     | 200 · 403 si no es tu usuario · 409 nombre repetido |
| DELETE | `/api/usuarios/{id}`     | 204 · 403 si no es tu usuario · 409 si tiene misiones |

**/api/misiones**

| Método | Ruta                     | Respuesta |
|--------|--------------------------|-----------|
| GET    | `/api/misiones`          | 200 solo tus misiones |
| GET    | `/api/misiones/{id}`     | 200 · 404 si no existe o es de otro usuario |
| POST   | `/api/misiones`          | 201 creada (el usuarioId del JSON se ignora) · 400 datos inválidos |
| PUT    | `/api/misiones/{id}`     | 200 · 404 si no existe o es de otro usuario |
| DELETE | `/api/misiones/{id}`     | 204 · 404 si no existe o es de otro usuario |

Sin sesión, todos (excepto login/logout) responden **401**.

`/login` y `/logout` se añadieron para poder usar la API sin el formulario web, ya que los
formularios MVC exigen un token antiforgery que Postman no tiene.

---

## 7. Comandos utilizados

```bash
dotnet --version                                   # 10.0.403
dotnet new mvc -n AstroMissionMVC -o <carpeta temporal> --no-restore   # plantilla MVC de referencia
psql -h localhost -U postgres -d postgres -f Database/01_crear_base.sql
psql -h localhost -U postgres -d AstroMissionMVC -f Database/02_tablas_y_datos.sql
dotnet build
dotnet run                                         # http://localhost:5009
```

`psql` está en `C:\Program Files\PostgreSQL\17\bin\psql.exe`. En pgAdmin se pueden ejecutar los
mismos scripts con el Query Tool.

Npgsql 10.0.3 ya estaba en el proyecto: solo se pasó la referencia al nuevo `AstroMissionMVC.csproj`.

---

## 8. Posibles errores comunes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| "No se pudo conectar con la base de datos" | PostgreSQL apagado | Iniciar el servicio `postgresql-x64-17` (services.msc) |
| `28P01 password authentication failed` | Contraseña distinta a 1234 | Corregir `appsettings.json` |
| `3D000 database "AstroMissionMVC" does not exist` | Base no creada o creada sin comillas (quedó en minúsculas) | Ejecutar `01_crear_base.sql` |
| `42P01 relation "misiones" does not exist` | Faltan las tablas | Ejecutar `02_tablas_y_datos.sql` conectado a AstroMissionMVC |
| `Failed to bind to address ... 5009` | Ya hay otra instancia corriendo | Cerrar la otra terminal o `Ctrl+C` |
| "Failed to determine the https port" | Se ejecuta solo con http | Es solo una advertencia, no afecta |
| Siempre vuelve al login | La sesión expiró (30 min) o `UseSession()` mal ubicado | Volver a iniciar sesión / revisar Program.cs |
| Error 400 al enviar un formulario | Falta el token antiforgery | Usar siempre `<form asp-...>` (lo agrega solo) |
| Error de compilación por namespace | Se cambió el nombre del proyecto | Mantener `RootNamespace` = AstroMissionMVC |

---

## 9. Cómo funciona cada parte

### MVC
- **Modelo** (`Models/`): `Usuario`, `Mision`, `LoginViewModel`. Representan los datos y sus
  validaciones (`[Required]`, `[StringLength]`, `[RegularExpression]`).
- **Vista** (`Views/*.cshtml`): HTML con Razor. Usan `@model` y tag helpers (`asp-for`, `asp-action`).
- **Controlador** (`Controllers/`): recibe la petición, llama al repositorio y elige la vista.

### API
- Carpeta `Api/`, clases con `[ApiController]` y `[Route("api/...")]` (ruteo por atributos).
- Heredan de `ControllerBase` (sin vistas) y devuelven JSON con `Ok()`, `NotFound()`, `Created...`.
- Usan los **mismos repositorios** que MVC: el SQL está en un solo lugar.

### Conexión con PostgreSQL
1. `appsettings.json` → `ConnectionStrings:PostgreSQL`.
2. `ConexionDB` lee esa cadena **una vez** y entrega `NpgsqlConnection`.
3. `Program.cs` registra `ConexionDB`, `UsuarioRepository` y `MisionRepository` (inyección de dependencias).
4. Los controladores reciben el repositorio por el constructor.
5. El repositorio abre la conexión, ejecuta `NpgsqlCommand` con **parámetros** (`@id`) y la cierra con `using`.

### Sesión
- `Program.cs`: `AddDistributedMemoryCache()` + `AddSession()` + `app.UseSession()`.
- Login correcto → `Session.SetInt32("UsuarioId", …)` y `Session.SetString("NombreUsuario", …)`.
- `MisionesController.OnActionExecuting` revisa la sesión antes de **cada** acción; si no hay, redirige a `/Login`.
- El layout muestra el nombre y el botón "Cerrar sesión" si hay sesión.
- Logout → `Session.Clear()` y redirección al login.

### Seguridad: cómo se impide ver o modificar misiones ajenas
1. El formulario **no tiene** campo UsuarioId, y `[BindNever]` impide que MVC lo lea aunque alguien lo envíe.
2. El controlador asigna `mision.UsuarioId = UsuarioIdSesion` (de la sesión, nunca del formulario o JSON).
3. **Todas** las consultas filtran por el dueño:
   `SELECT ... WHERE id = @id AND usuarioid = @usuarioId`
   `UPDATE ... WHERE id = @id AND usuarioid = @usuarioId`
   `DELETE ... WHERE id = @id AND usuarioid = @usuarioId`
   Si la misión es de otro usuario, la consulta no encuentra nada → mensaje "no existe o no pertenece a tu usuario".
4. Parámetros de Npgsql en todo el SQL → no hay concatenación → se evita la inyección SQL.
5. Formularios POST con `[ValidateAntiForgeryToken]`.

---

## 10. Explicación corta para el examen

> AstroMissionMVC es una aplicación ASP.NET Core MVC conectada a PostgreSQL con Npgsql, sin
> Entity Framework: escribo el SQL a mano con parámetros. Tiene dos tablas, `usuarios` y `misiones`,
> con una relación 1 a N mediante la llave foránea `misiones.usuarioid → usuarios.id`.
>
> El usuario inicia sesión en `LoginController`, que valida usuario y contraseña contra PostgreSQL
> y guarda su Id y nombre en la sesión de ASP.NET Core. `MisionesController` revisa la sesión
> antes de cada acción y hace el CRUD usando siempre el UsuarioId de la sesión, nunca uno enviado
> desde el formulario. Todas las consultas incluyen `WHERE usuarioid = @usuarioId`, así que un
> usuario no puede ver, editar ni borrar misiones de otro.
>
> La separación es clara: `Controllers` devuelve vistas Razor, `Api` devuelve JSON en
> `/api/usuarios` y `/api/misiones` con GET, GET por id, POST, PUT y DELETE, y `Data` concentra
> la conexión y todo el SQL, que reutilizan tanto MVC como la API.
