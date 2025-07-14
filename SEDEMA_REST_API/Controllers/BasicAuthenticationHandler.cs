using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using DotNetEnv; // para cargar el .env
using static SEDEMA_REST_API.Errores.Excepcion;
using SEDEMA_REST_API.BDD;


namespace SEDEMA_REST_API.Controllers {
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
        public event ErrorProcesoEventHandler? ErrorProceso;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock) {

            // Cargar .env solo si no se ha cargado antes
            try {
                string baseDir = AppContext.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
                string envPath = Path.Combine(projectRoot, ".env");

                if (File.Exists(envPath)) {
                    DotNetEnv.Env.Load(envPath);
                }
            } catch (Exception ex) {
                ErrorProceso?.Invoke(new ErrorProcesoArgs(
                    "SEDEMA_REST_API",
                    nameof(BasicAuthenticationHandler),
                    this.GetType().Name,
                    ex.HResult,
                    ex.Message,
                    0
                ));
                Logger.LogWarning($"No se pudo cargar el archivo .env: {ex.Message}");
            }
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization Header");

            try {
                var authHeader = Request.Headers["Authorization"].ToString();
                var authHeaderValue = authHeader.Substring("Basic ".Length).Trim();
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderValue)).Split(':');

                if (credentials.Length != 2)
                    return AuthenticateResult.Fail("Invalid Authorization Format");

                var username = credentials[0];
                var password = credentials[1];

                

                // Leer credencial esperada desde variables de entorno
                string? expectedPassword = Environment.GetEnvironmentVariable($"BASIC_AUTH_{username.ToUpper()}");
                //Console.WriteLine(Environment.GetEnvironmentVariable("BASIC_AUTH_ADMIN"));
                Logger.LogInformation($"Credencial cargada: {expectedPassword}");

                if (string.IsNullOrWhiteSpace(expectedPassword) || expectedPassword != password) {
                    return AuthenticateResult.Fail("Invalid Username or Password");
                }
                if (expectedPassword == null)
                    return AuthenticateResult.Fail("Usuario no permitido");

                if (expectedPassword != password)
                    return AuthenticateResult.Fail("Contraseña incorrecta");
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(ClaimTypes.Name, username),
                };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }  catch (Exception ex) {
                ErrorProceso?.Invoke(new ErrorProcesoArgs(
                    "SEDEMA_REST_API",
                    nameof(BasicAuthenticationHandler),
                    this.GetType().Name,
                    ex.HResult,
                    ex.Message,
                    0
                ));
                Logger.LogError(ex, "Error durante la autenticación básica");
                return AuthenticateResult.Fail("Authorization processing error");
            }
        }
    }
}
