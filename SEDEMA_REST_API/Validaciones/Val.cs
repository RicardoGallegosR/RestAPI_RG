namespace SEDEMA_REST_API.Validaciones {
    public class Val {
        public static bool Folios(int folioInicial, int folioFinal) {
            return folioInicial > 0 && folioFinal > 0 && folioInicial < folioFinal;
        }


    }
}
