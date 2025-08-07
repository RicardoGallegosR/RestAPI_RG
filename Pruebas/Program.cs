using Microsoft.Extensions.Logging.Abstractions;
using Seguridad.Configuracion;
using Seguridad.Servicios;
using Seguridad.Hashing;
using System.Text.Json;

namespace MiConsolaApp {
    class Program {
        static async Task Main(string[] args) {

            //*
            var conexion = new Conexion(); // Asegúrate de tener un constructor sin parámetros
            var logger = NullLogger<UsuarioValidador>.Instance;

            var usuarioValidador = new UsuarioValidador(conexion, logger);
            var json = await usuarioValidador.ObtenerHashPorUsuarioJSONAsync("MORELOS");


            var lista = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);

            if (lista != null && lista.Count > 0 && lista[0].TryGetValue("ContraseñaHash", out var hash)) {
                Console.WriteLine($"Hash: {hash}");
            } else {
                Console.WriteLine("Credencial no encontrada.");
            }
            //*/

            //Console.WriteLine($"Contraseña nueva: {HashUtils.GenerarHash("rroGYmTT$e5r3e^Dh^k3F3fcWLuMzc")}");

        }
    }
}
