using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;


namespace SEDEMA_REST_API.Validaciones {
    public class Val {
        public static bool Folios(int folioInicial, int folioFinal) {
            return folioInicial > 0 && folioFinal > 0 && folioInicial < folioFinal;
        }
        
        
        // Expresión regular: solo letras A-H, J-N, P, R-Z y números, sin I, O, Q, Ñ
        public static bool Placa(string placa) {
            if (string.IsNullOrWhiteSpace(placa))
                return false;
            placa = placa.ToUpper();
            if (placa.Length > 11)
                return false;
            var regex = new Regex("^[A-HJ-NPR-Z0-9]+$");
            return regex.IsMatch(placa);
        }


        public static bool Vin(string Vin) {
            if (string.IsNullOrWhiteSpace(Vin))
                return false;
            Vin = Vin.ToUpper();
            if (Vin.Length < 11 || Vin.Length > 17)
                return false;
            var regex = new Regex("^[A-HJ-NPR-Z0-9]+$");
            return regex.IsMatch(Vin);
        }
    }
}
