using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SEDEMA_REST_API.Errores;
using static SEDEMA_REST_API.Errores.Excepcion;
using System.Text.Json;


namespace SEDEMA_REST_API.BDD {
    public class ResultadoCertificado {
        public int MensajeId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }


    public class CertificadoTipo {
        public int TipoId { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    public class CertificadoDisponible {
        public int Remision { get; set; }
        public int FolioInicialDisponible { get; set; }
        public int FolioFinalDisponible { get; set; }
        public int CantidadDisponible { get; set; }
        public string Holograma { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
    }


    public class spSivev {
        public event ErrorProcesoEventHandler? ErrorProceso;
        private readonly Conexion _conexion;
        private readonly ILogger<spSivev> _logger;

        public spSivev(Conexion conexion, ILogger<spSivev> logger) {
            _conexion = conexion;
            _logger = logger;
        }
        public async Task<string> SpCertificadosAlmacenSet(int folioInicial, int folioFinal, int certificadoTipoId) {
            var resultados = new List<ResultadoCertificado>();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringSIVEV()))
            using (SqlCommand cmd = new SqlCommand("SivSpComun.SpCertificadosAlmacenSet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FolioInicial", folioInicial);
                cmd.Parameters.AddWithValue("@FolioFinal", folioFinal);
                cmd.Parameters.AddWithValue("@CertificadoTipoId", certificadoTipoId);

                try {
                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) {
                        if (await reader.ReadAsync()) {
                            var mensajeIdOrdinal = reader.GetOrdinal("MensajeId");
                            var mensajeOrdinal = reader.GetOrdinal("Mensaje");

                            resultados.Add(new ResultadoCertificado {
                                MensajeId = reader.IsDBNull(mensajeIdOrdinal) ? -1 : reader.GetInt32(mensajeIdOrdinal),
                                Mensaje = reader.IsDBNull(mensajeOrdinal) ? "Sin mensaje" : reader.GetString(mensajeOrdinal)
                            });
                        }

                    }
                } catch (Exception ex) {
                    resultados.Add(new ResultadoCertificado {
                        MensajeId = ex.HResult,
                        Mensaje = ex.Message
                    });
                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "SEDEMA_REST_API",
                        nameof(spSivev),
                        this.GetType().Name,
                        ex.HResult,
                        ex.Message,
                        0
                    ));
                }
            }

            return JsonSerializer.Serialize(resultados);
        }

        public async Task<string> SpCertificadosDisponiblesGetJSON(int certificadoTipoId) {
            var dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringSIVEV()))
            using (SqlCommand cmd = new SqlCommand("SivSpComun.SpCertificadosDisponiblesGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CertificadoTipoId", certificadoTipoId);

                try {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        dt.Load(reader);
                    }

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
                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "SEDEMA_REST_API",
                        nameof(spSivev),
                        this.GetType().Name,
                        ex.HResult,
                        ex.Message,
                        0
                    ));
                    _logger.LogWarning($"Error al ejecutar SpCertificadosDisponiblesGet: {ex.Message}");
                    return "[]";
                }
            }
        }

        public async Task<string> SpAppAltaCertificados(short verificentroId, byte tipoId, int folioInicial, int folioFinal) {
            var resultados = new List<ResultadoCertificado>();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringSIVEV()))
            using (SqlCommand cmd = new SqlCommand("Certificados.SpAppAltaCertificados", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@siVerificentroId", verificentroId);
                cmd.Parameters.AddWithValue("@tiTipoId", tipoId);
                cmd.Parameters.AddWithValue("@iFolioInicial", folioInicial);
                cmd.Parameters.AddWithValue("@iFolioFinal", folioFinal);

                // Parámetros de salida
                var mensajeParam = new SqlParameter("@vcMensaje", SqlDbType.NVarChar, 100) {
                    Direction = ParameterDirection.Output
                };
                var resultadoParam = new SqlParameter("@iResultado", SqlDbType.Int) {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(mensajeParam);
                cmd.Parameters.Add(resultadoParam);

                try {
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    resultados.Add(new ResultadoCertificado {
                        MensajeId = (int)(resultadoParam.Value ?? -1),
                        Mensaje = mensajeParam.Value?.ToString() ?? "Sin mensaje"
                    });

                } catch (Exception ex) {
                    resultados.Add(new ResultadoCertificado {
                        MensajeId = ex.HResult,
                        Mensaje = ex.Message
                    });

                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "SEDEMA_REST_API",
                        nameof(spSivev),
                        this.GetType().Name,
                        ex.HResult,
                        ex.Message,
                        0
                    ));
                }
            }

            return JsonSerializer.Serialize(resultados);
        }

        public async Task<string> SpVerificentrosActivosGetJSON() {
            var dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringSIVEV()))
            using (SqlCommand cmd = new SqlCommand("Verificentros.VerificentrosGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                try {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        dt.Load(reader);
                    }

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
                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "SEDEMA_REST_API",
                        nameof(spSivev),
                        this.GetType().Name,
                        ex.HResult,
                        ex.Message,
                        0
                    ));
                    _logger.LogWarning($"Error al ejecutar SpVerificentrosActivosGetJSON: {ex.Message}");
                    return "[]";
                }
            }
        }



        public async Task<string> SpCertificadosTiposGetJSON() {
            var dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringSIVEV()))
            using (SqlCommand cmd = new SqlCommand("SivSpComun.SpCertificadosTiposGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;

                try {
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        dt.Load(reader);
                    }

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
                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "SEDEMA_REST_API",
                        nameof(spSivev),
                        this.GetType().Name,
                        ex.HResult,
                        ex.Message,
                        0
                    ));
                    _logger.LogWarning($"Error al ejecutar SpCertificadosTiposGet: {ex.Message}");
                    return "[]";
                }
            }
        }

        public async Task<string> SpCertificadosTiposMaxGet(int certificadoTipoId) {
            var resultados = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringSIVEV()))
            using (SqlCommand cmd = new SqlCommand("SivSpComun.SpCertificadosTiposMaxGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CertificadoTipoId", certificadoTipoId);

                try {
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            var resultado = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++) {
                                resultado[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            }
                            resultados.Add(resultado);
                        }
                    }
                } catch (Exception ex) {
                    resultados.Add(new Dictionary<string, object> {
                { "MensajeId", ex.HResult },
                { "Mensaje", ex.Message }
            });

                    ErrorProceso?.Invoke(new ErrorProcesoArgs(
                        "SEDEMA_REST_API",
                        nameof(spSivev),
                        this.GetType().Name,
                        ex.HResult,
                        ex.Message,
                        0
                    ));
                }
            }

            return JsonSerializer.Serialize(resultados);
        }


    }
}
