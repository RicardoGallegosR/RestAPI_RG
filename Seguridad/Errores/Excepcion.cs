namespace Seguridad.Errores {
    public class Excepcion {
        public delegate void ErrorProcesoEventHandler(ErrorProcesoArgs args);
        public event ErrorProcesoEventHandler? ErrorProceso;

        public record ErrorProcesoArgs(
            string Libreria,
            string Clase,
            string Metodo,
            int ErrNum,
            string ErrDesc,
            int SqlErr
        ) {
            public override string ToString() =>
                $"[{Libreria}.{Clase}.{Metodo}] Error {ErrNum}: {ErrDesc} (SQL:{SqlErr})";
        }
    }
}
