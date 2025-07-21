using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Seguridad.Configuracion;
using System.Data;
using System.Text.Json;
using static Seguridad.Errores.Excepcion;

namespace Seguridad.Servicios {
    public class UsuarioValidador {
        public event ErrorProcesoEventHandler? ErrorProceso;
        private readonly Conexion _conexion;
        private readonly ILogger<UsuarioValidador> _logger;

        public UsuarioValidador(Conexion conexion, ILogger<UsuarioValidador> logger) {
            _conexion = conexion;
            _logger = logger;
        }

        public async Task<string> ObtenerHashPorUsuarioJSONAsync(string usuario) {
            var dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringMonitoreo()))
            using (SqlCommand cmd = new SqlCommand("Auth.spUsuarioLoginGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Usuario", usuario);

                try {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        dt.Load(reader);
                    }
                    return JsonSerializer.Serialize(ConvertirDataTableAJson(dt));

                } catch (Exception ex) {
                    int sqlErr = (ex is SqlException sqlEx) ? sqlEx.Number : 0;

                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "Seguridad",
                        nameof(UsuarioValidador),
                        GetType().Name,
                        ex.HResult,
                        ex.Message,
                        sqlErr
                    ));
                    Console.WriteLine($"Error al ejecutar ObtenerHashPorUsuarioJSONAsync: {ex.Message}");
                    _logger.LogWarning($"Error al ejecutar ObtenerHashPorUsuarioJSONAsync: {ex.Message}");
                    return "[]";
                }
            }
        }

        public async Task<bool> RegistrarIntentoYVerificarBloqueoAsync(int credencialId) {
            await SpBloqueoUsuarioSetAsync(credencialId);
            var json = await SpBloqueoUsuarioGetJSON(credencialId);
            if (!string.IsNullOrWhiteSpace(json) && json != "[]") {
                var bloqueos = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                if (bloqueos?.Count > 0 &&
                    bloqueos[0].TryGetValue("BloqueadoHasta", out var hastaObj) &&
                    DateTime.TryParse(hastaObj?.ToString(), out var bloqueadoHasta) &&
                    bloqueadoHasta > DateTime.Now) {
                    return true;
                }
            }
            return false;
        }



        public async Task<string> SpBloqueoUsuarioGetJSON(int credencialId) {
            var dt = new DataTable();

            using var conn = new SqlConnection(_conexion.GetConnectionStringMonitoreo());
            using var cmd = new SqlCommand("Auth.spBloqueoUsuarioGet", conn) {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CredencialId", credencialId);

            try {
                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                dt.Load(reader);
                return JsonSerializer.Serialize(ConvertirDataTableAJson(dt));
            } catch (Exception ex) {
                int sqlErr = (ex is SqlException sqlEx) ? sqlEx.Number : 0;

                ErrorProceso?.Invoke(new ErrorProcesoArgs(
                    "Seguridad",
                    nameof(SpBloqueoUsuarioGetJSON),
                    this.GetType().Name,
                    ex.HResult,
                    ex.Message,
                    sqlErr
                ));
                Console.WriteLine($"Error al ejecutar SpBloqueoUsuarioGetJSON: {ex.Message}");
                _logger.LogWarning($"Error en SpBloqueoUsuarioGetJSON: {ex.Message}");
                return "[]";
            }
        }

        public async Task<bool> SpBloqueoUsuarioSetAsync(int credencialId, int limiteIntentos = 5, int minutosBloqueo = 60) {
            using var conn = new SqlConnection(_conexion.GetConnectionStringMonitoreo());
            using var cmd = new SqlCommand("Auth.spBloqueoUsuarioSet", conn) {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CredencialId", credencialId);
            cmd.Parameters.AddWithValue("@LimiteIntentos", limiteIntentos);
            cmd.Parameters.AddWithValue("@MinutosBloqueo", minutosBloqueo);

            try {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return true;
            } catch (Exception ex) {
                int sqlErr = (ex is SqlException sqlEx) ? sqlEx.Number : 0;

                ErrorProceso?.Invoke(new ErrorProcesoArgs(
                    "Seguridad",
                    nameof(SpBloqueoUsuarioSetAsync),
                    this.GetType().Name,
                    ex.HResult,
                    ex.Message,
                    sqlErr
                ));
                Console.WriteLine($"Error al ejecutar SpBloqueoUsuarioSetAsync: {ex.Message}");
                _logger.LogWarning($"Error al ejecutar SpBloqueoUsuarioSetAsync: {ex.Message}");
                return false;
            }
        }


        private static List<Dictionary<string, object>> ConvertirDataTableAJson(DataTable dt) {
            var lista = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows) {
                var item = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns) {
                    item[col.ColumnName] = row[col];
                }
                lista.Add(item);
            }
            return lista;
        }
    }

    public  class CredencialHash {
        public int Credencial { get; set; }
        public string ContraseñaHash { get; set; } = string.Empty;
    }

}
