using DotNetEnv;

namespace Seguridad.Configuracion {
    public class Conexion {
        public Conexion() {
            string baseDir = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            string envPath = Path.Combine(projectRoot, ".env");

            if (File.Exists(envPath)) {
                Env.Load(envPath);
                Console.WriteLine($".env cargado correctamente desde: {envPath}");
            } else {
                Console.WriteLine($".env NO encontrado en: {envPath}. Usando variables del entorno del sistema.");
            }
        }


        public string GetConnectionStringSIVEV() =>
            GetConnectionString("DB_SIVEV_SERVER", "DB_SIVEV_NAME", "DB_SIVEV_USER", "DB_SIVEV_PASS");

        public string GetConnectionStringMonitoreo() =>
            GetConnectionString("DB_MONITOREO_SERVER", "DB_MONITOREO_NAME", "DB_MONITOREO_USER", "DB_MONITOREO_PASS");

        private string GetConnectionString(string serverKey, string dbKey, string userKey, string passKey) {
            string server = Environment.GetEnvironmentVariable(serverKey);
            string database = Environment.GetEnvironmentVariable(dbKey);
            string user = Environment.GetEnvironmentVariable(userKey);
            string pass = Environment.GetEnvironmentVariable(passKey);

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass)) {
                throw new InvalidOperationException($"Faltan variables de entorno: {serverKey}, {dbKey}, {userKey}, {passKey}");
            }

            return $"Data Source={server};Initial Catalog={database};User ID={user};Password={pass};TrustServerCertificate=True;";
        }
    }
}
