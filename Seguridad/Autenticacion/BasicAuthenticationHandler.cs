using DotNetEnv; 
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
//using SEDEMA_REST_API.BDD;
using Seguridad.Hashing;
using Seguridad.Servicios;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static Seguridad.Errores.Excepcion;



namespace Seguridad.Autenticacion {
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
        public event ErrorProcesoEventHandler? ErrorProceso;
        private static readonly Dictionary<string, int> _failedAttemptsByIp = new();
        private static readonly Dictionary<string, DateTime> _blockedIps = new();
        private static readonly object _lock = new();

        private readonly UsuarioValidador _usuarioValidador;
        private readonly ILogger<BasicAuthenticationHandler> _logger;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock,
            UsuarioValidador usuarioValidador)
            : base(options, loggerFactory, encoder, clock) {
            _usuarioValidador = usuarioValidador;
            _logger = loggerFactory.CreateLogger<BasicAuthenticationHandler>();
        }
        /*
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
                    Console.WriteLine("Archivo Carcgado");
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
        */




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
                var ip = Context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";


                Console.WriteLine($"Muestro la credencial de pruebas {username}, el password {password} con ip {ip}");


                lock (_lock) {
                    if (_blockedIps.TryGetValue(ip, out var blockedUntil)) {
                        if (DateTime.UtcNow < blockedUntil) {
                            _logger.LogWarning($"IP {ip} bloqueada hasta {blockedUntil}");
                            return AuthenticateResult.Fail("IP bloqueada por múltiples intentos fallidos.");
                        } else {
                            _blockedIps.Remove(ip);
                            _failedAttemptsByIp.Remove(ip);
                        }
                    }
                }
                var credencialJson = await _usuarioValidador.ObtenerHashPorUsuarioJSONAsync(username);

                if (string.IsNullOrWhiteSpace(credencialJson) || credencialJson == "[]") {
                    Console.WriteLine("El usuario no existe.");
                    return AuthenticateResult.Fail("Usuario no válido");
                }

                // Deserializar
                var lista = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(credencialJson);

                if (lista == null || lista.Count == 0 || !lista[0].TryGetValue("ContraseñaHash", out var hashObj)) {
                    Console.WriteLine("La credencial está vacía o no tiene campo ContraseñaHash.");
                    return AuthenticateResult.Fail("Usuario no válido");
                }

                string hash = hashObj?.ToString() ?? "";

                if (hash != password) {
                    lock (_lock) {
                        if (_failedAttemptsByIp.ContainsKey(ip))
                            _failedAttemptsByIp[ip]++;
                        else
                            _failedAttemptsByIp[ip] = 1;

                        if (_failedAttemptsByIp[ip] >= 5) {
                            var bloqueoHasta = DateTime.UtcNow.AddHours(1);
                            _blockedIps[ip] = bloqueoHasta;
                            Console.WriteLine($"[BasicAuth] IP {ip} bloqueada hasta {bloqueoHasta} UTC");
                        }
                    }
                    return AuthenticateResult.Fail("Usuario o contraseña inválidos.");
                }

                // Aquí podrías retornar éxito si todo está bien


                var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
            };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            } catch (Exception ex) {
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
