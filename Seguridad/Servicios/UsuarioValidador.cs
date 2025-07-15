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

                    if (dt.Rows.Count == 0)
                        return "[]";

                    var jsonList = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows) {
                        var json = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns) {
                            json[col.ColumnName] = row[col];
                        }
                        jsonList.Add(json);
                    }

                    return JsonSerializer.Serialize(jsonList);
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
                    Console.WriteLine($"Error al ejecutar ObtenerHashPorUsuarioAsync: {ex.Message}");
                    _logger.LogWarning($"Error al ejecutar ObtenerHashPorUsuarioAsync: {ex.Message}");
                    return "[]";
                }
            }
        }

    }

    public  class CredencialHash {
        public int Credencial { get; set; }
        public string ContraseñaHash { get; set; } = string.Empty;
    }

}
