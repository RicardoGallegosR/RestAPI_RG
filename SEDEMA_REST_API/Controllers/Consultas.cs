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
        [HttpGet("Placa")]
        public IActionResult PlacaGet2() {
            if (User.Identity != null && User.Identity.IsAuthenticated) {
                return Redirect("https://www.sedema.cdmx.gob.mx/secretaria/estructura/199");
            }
            return Redirect("https://www.sedema.cdmx.gob.mx/secretaria/estructura/20");
        }

        [HttpGet("NIV")]
        public IActionResult VINGet() {
            if (User.Identity != null && User.Identity.IsAuthenticated) {
                return Redirect("https://www.sedema.cdmx.gob.mx/secretaria/estructura/199");
            }

            return Redirect("https://www.sedema.cdmx.gob.mx/secretaria/estructura/20");
        }



        [HttpPost("Placa")]
        [Authorize]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> PlacaGet([FromForm] string placa) {
            if (string.IsNullOrWhiteSpace(placa))
                return BadRequest(new { mensaje = "La placa no puede estar vacía." });
            placa = placa.ToUpper();
            if (!Val.Placa(placa))
                return BadRequest(new { mensaje = "Placa inválida." });
            string resultadoJson = await _spSivev.newSpSWUltimaVerificacionPlacaGet(placa);
            if (string.IsNullOrWhiteSpace(resultadoJson) || resultadoJson == "[]")
                return NotFound(new { mensaje = $"No se encontraron verificaciones para la placa {placa}." });
            var resultado = JsonSerializer.Deserialize<List<ResultadoVerificacion>>(resultadoJson);
            if (resultado == null || !resultado.Any())
                return StatusCode(500, new { mensaje = "No se pudo procesar la respuesta del servidor." });

            if (resultado.All(r => r.MensajeId == "200"))
                return Ok(resultado);
            var error = resultado.FirstOrDefault(r => r.MensajeId != "200");
            if (error != null)
                return BadRequest(new { mensaje = error.Mensaje });
            return StatusCode(500, new { mensaje = "Error interno al procesar la solicitud." });
        }





        [HttpPost("NIV")]
        [Authorize]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> NIVGet([FromForm] string NIV) {
            if (string.IsNullOrWhiteSpace(NIV))
                return BadRequest(new { mensaje = "La placa no puede estar vacía." });
            NIV = NIV.ToUpper();
            if (!Val.Vin(NIV))
                return BadRequest(new { mensaje = "NIV inválido." });
            string resultadoJson = await _spSivev.newSpSWUltimaVerificacionVINGet(NIV);
            if (string.IsNullOrWhiteSpace(resultadoJson) || resultadoJson == "[]")
                return NotFound(new { mensaje = $"No se encontraron verificaciones para el NIV {NIV}." });
            var resultado = JsonSerializer.Deserialize<List<ResultadoVerificacion>>(resultadoJson);
            if (resultado == null || !resultado.Any())
                return StatusCode(500, new { mensaje = "No se pudo procesar la respuesta del servidor." });

            if (resultado.All(r => r.MensajeId == "200"))
                return Ok(resultado);
            var error = resultado.FirstOrDefault(r => r.MensajeId != "200");
            if (error != null)
                return BadRequest(new { mensaje = error.Mensaje });
            return StatusCode(500, new { mensaje = "Error interno al procesar la solicitud." });
        }


    }
}
