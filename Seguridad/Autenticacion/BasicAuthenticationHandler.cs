using DotNetEnv; 
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization Header");

            try {
                var authHeader = Request.Headers["Authorization"].ToString();
                var authHeaderValue = authHeader.Substring("Basic ".Length).Trim();
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderValue)).Split(':');

                if (credentials.Length != 2)
                    return AuthenticateResult.Fail("Formato de autorización inválido");

                var username = credentials[0];
                var password = credentials[1];
                var ip = Context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

                // 1. Validar si la IP está bloqueada
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

                // 2. Obtener credencial y hash
                var credencialJson = await _usuarioValidador.ObtenerHashPorUsuarioJSONAsync(username);

                if (string.IsNullOrWhiteSpace(credencialJson) || credencialJson == "[]") {
                    return AuthenticateResult.Fail("Usuario no válido");
                }

                var lista = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(credencialJson);
                if (lista is null || lista.Count == 0) {
                    return AuthenticateResult.Fail("Usuario no válido");
                }

                int credencialId = int.Parse(lista[0]["CredencialId"].ToString());
                string hash = lista[0]["ContraseñaHash"]?.ToString() ?? "";

                // 🔐 VERIFICAMOS PRIMERO SI EL USUARIO YA ESTÁ BLOQUEADO
                var bloqueoJson = await _usuarioValidador.SpBloqueoUsuarioGetJSON(credencialId);
                var bloqueos = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(bloqueoJson);

                if (bloqueos?.Count > 0 &&
                    bloqueos[0].TryGetValue("BloqueadoHasta", out var hastaObj) &&
                    DateTime.TryParse(hastaObj?.ToString(), out var bloqueadoHasta) &&
                    bloqueadoHasta > DateTime.Now) {

                    _logger.LogWarning($"Usuario {username} bloqueado hasta {bloqueadoHasta}");
                    return AuthenticateResult.Fail("Usuario temporalmente bloqueado.");
                }


                // 3. Verificar contraseña
                if (!HashUtils.ValidarHash(password, hash)) {
                    await _usuarioValidador.SpBloqueoUsuarioSetAsync(credencialId);

                    // Si no está bloqueado, seguir mostrando credenciales inválidas
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

                // 4. Autenticación exitosa
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
