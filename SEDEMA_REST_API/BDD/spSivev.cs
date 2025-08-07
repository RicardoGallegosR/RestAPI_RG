using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SEDEMA_REST_API.Errores;
using static SEDEMA_REST_API.Errores.Excepcion;
using System.Text.Json;


namespace SEDEMA_REST_API.BDD {
    public class ResultadoVerificacion {
        public int Verificentro { get; set; }
        public string Placa { get; set; }
        public string NIV { get; set; }
        public string Marca { get; set; }
        public string Submarca { get; set; }
        public int Modelo { get; set; }
        public string Combustible { get; set; }
        public string FechaVerificacion { get; set; }
        public int Certificado { get; set; }
        public string Holograma { get; set; }
        public string Vigencia { get; set; }
        public string MensajeId { get; set; }
        public string Mensaje { get; set; }

    }



    public class spSivev {
        public event ErrorProcesoEventHandler? ErrorProceso;
        private readonly Conexion _conexion;
        private readonly ILogger<spSivev> _logger;

        public spSivev(Conexion conexion, ILogger<spSivev> logger) {
            _conexion = conexion;
            _logger = logger;
        }
        public async Task<string> newSpSWUltimaVerificacionPlacaGet(string vcPlacaId) {
            var resultados = new List<ResultadoVerificacion>();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringMorelos()))
            using (SqlCommand cmd = new SqlCommand("LabCdMexico.newSpSWUltimaVerificacionGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@vcPlacaId", vcPlacaId);
                cmd.Parameters.AddWithValue("@vcVehiculoId", DBNull.Value);
                try {
                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            resultados.Add(new ResultadoVerificacion {
                                Verificentro = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Verificentro"))),
                                Placa = reader.GetString(reader.GetOrdinal("Placa")),
                                NIV = reader.GetString(reader.GetOrdinal("NIV")),
                                Marca = reader.GetString(reader.GetOrdinal("Marca")),
                                Submarca = reader.GetString(reader.GetOrdinal("Submarca")),
                                Modelo = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Modelo"))),
                                Combustible = reader.GetString(reader.GetOrdinal("Combustible")),
                                FechaVerificacion = reader.GetString(reader.GetOrdinal("FechaVerificacion")),
                                Certificado = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Certificado"))),
                                Holograma = reader.GetString(reader.GetOrdinal("Holograma")),
                                Vigencia = reader.GetString(reader.GetOrdinal("Vigencia")),
                                MensajeId = "200",
                                Mensaje = "Correcto"
                            });
                        }
                    }
                } catch (Exception ex) {
                    resultados.Add(new ResultadoVerificacion {
                        MensajeId = ex.HResult.ToString(),
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
                    Console.WriteLine($"Error al ejecutar newSpSWUltimaVerificacionPlacaGet : {ex.Message}");
                    _logger.LogWarning($"Error al ejecutar newSpSWUltimaVerificacionPlacaGet : {ex.Message}");
                    return "[]";
                }
            }

            return JsonSerializer.Serialize(resultados);
        }

        public async Task<string> newSpSWUltimaVerificacionVINGet(string vcVehiculoId) {
            var resultados = new List<ResultadoVerificacion>();

            using (SqlConnection conn = new SqlConnection(_conexion.GetConnectionStringMorelos()))
            using (SqlCommand cmd = new SqlCommand("LabCdMexico.newSpSWUltimaVerificacionGet", conn)) {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@vcPlacaId", DBNull.Value);
                cmd.Parameters.AddWithValue("@vcVehiculoId", vcVehiculoId);
                try {
                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            resultados.Add(new ResultadoVerificacion {
                                Verificentro = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Verificentro"))),
                                Placa = reader.GetString(reader.GetOrdinal("Placa")),
                                NIV = reader.GetString(reader.GetOrdinal("NIV")),
                                Marca = reader.GetString(reader.GetOrdinal("Marca")),
                                Submarca = reader.GetString(reader.GetOrdinal("Submarca")),
                                Modelo = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Modelo"))),
                                Combustible = reader.GetString(reader.GetOrdinal("Combustible")),
                                FechaVerificacion = reader.GetString(reader.GetOrdinal("FechaVerificacion")),
                                Certificado = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Certificado"))),
                                Holograma = reader.GetString(reader.GetOrdinal("Holograma")),
                                Vigencia = reader.GetString(reader.GetOrdinal("Vigencia")),
                                MensajeId = "200",
                                Mensaje = "Correcto"
                            });
                        }
                    }
                } catch (Exception ex) {
                    resultados.Add(new ResultadoVerificacion {
                        MensajeId = ex.HResult.ToString(),
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
                    Console.WriteLine($"Error al ejecutar newSpSWUltimaVerificacionVINGet : {ex.Message}");
                    _logger.LogWarning($"Error al ejecutar newSpSWUltimaVerificacionVINGet : {ex.Message}");
                    return "[]";
                }
            }

            return JsonSerializer.Serialize(resultados);
        }
    }
}
