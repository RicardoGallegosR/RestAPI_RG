using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEDEMA_REST_API.BDD;
using SEDEMA_REST_API.Validaciones;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace SEDEMA_REST_API.Controllers {
    [Route("/[controller]")]
    public class Consultas : ControllerBase {
        private readonly spSivev _spSivev;

        public Consultas(spSivev spSivev) {
            _spSivev = spSivev;
        }
        [HttpPost("Almacen/HologramasSet")]
        [Authorize]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> InsertarEnAlmacen(
            [FromForm] int folioInicial,
            [FromForm] int folioFinal,
            [FromForm] int certificadoTipoId) {
            if (certificadoTipoId < 12 && Val.Folios(folioInicial, folioFinal)) {
                string resultadoJson = await _spSivev.SpCertificadosAlmacenSet(folioInicial, folioFinal, certificadoTipoId);

                var resultado = JsonSerializer.Deserialize<List<ResultadoCertificado>>(resultadoJson);

                if (resultado != null && resultado.Any()) {
                    var item = resultado.First();

                    if (item.MensajeId == 0)
                        return Ok(resultado);
                    return BadRequest(resultado);
                }
                return StatusCode(500, "No se obtuvo respuesta del procedimiento almacenado.");
            }

            return BadRequest("Datos inválidos. Verifique el rango de folios o el tipo de certificado.");
        }



        [HttpPost("Almacen/DisponiblesGet")]
        [Authorize]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> ObtenerDisponiblesEnAlmacen([FromForm] int certificadoTipoId) {
            if (certificadoTipoId >= 12) {
                return BadRequest(new { mensaje = "Tipo de certificado inválido." });
            }

            var json = await _spSivev.SpCertificadosDisponiblesGetJSON(certificadoTipoId);

            if (string.IsNullOrWhiteSpace(json) || json == "[]") {
                return NotFound(new { mensaje = "No se encontraron certificados disponibles para el tipo solicitado." });
            }

            return Content(json, "application/json");
        }


        [HttpPost("Verificentros/Activos")]
        [Authorize]
        public async Task<IActionResult> VerificentrosActivosGet() {
            var json = await _spSivev.SpVerificentrosActivosGetJSON();

            if (string.IsNullOrWhiteSpace(json) || json == "[]") {
                return NotFound("No se encontraron verificentros activos.");
            }
            return Content(json, "application/json");
        }



        [HttpPost("Almacen/Certificadotipo")]
        [Authorize]
        public async Task<IActionResult> CertificadosTiposGet() {
            var json = await _spSivev.SpCertificadosTiposGetJSON();

            if (string.IsNullOrWhiteSpace(json) || json == "[]") {
                return NotFound("No se encontraron tipos de certificados.");
            }
            return Content(json, "application/json");
        }



        [HttpPost("Certificados/Alta")]
        [Authorize]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> AltaCertificados(
            [FromForm] short verificentroId,
            [FromForm] byte tipoId,
            [FromForm] int folioInicial,
            [FromForm] int folioFinal) {

            if (verificentroId > 0 && tipoId > 0 && folioInicial > 0 && folioFinal >= folioInicial) {

                string resultadoJson = await _spSivev.SpAppAltaCertificados(verificentroId, tipoId, folioInicial, folioFinal);

                if (!string.IsNullOrWhiteSpace(resultadoJson) && resultadoJson != "[]") {
                    return Content(resultadoJson, "application/json");
                }

                return StatusCode(500, new { mensaje = "No se obtuvo respuesta del procedimiento almacenado." });
            }

            return BadRequest(new { mensaje = "Datos inválidos. Verifique el Verificentro, Tipo y Folios." });
        }



        [HttpPost("Certificados/FolioInicialSugerido")]
        [Authorize]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> ObtenerFolioInicialSugerido([FromForm] int certificadoTipoId) {
            if (certificadoTipoId <= 0) {
                return BadRequest(new { mensaje = "Tipo de certificado inválido." });
            }

            string resultadoJson = await _spSivev.SpCertificadosTiposMaxGet(certificadoTipoId);

            if (string.IsNullOrWhiteSpace(resultadoJson) || resultadoJson == "[]") {
                return NotFound(new { mensaje = "No se encontró el folio sugerido para el tipo solicitado." });
            }

            return Content(resultadoJson, "application/json");
        }





    }
}
