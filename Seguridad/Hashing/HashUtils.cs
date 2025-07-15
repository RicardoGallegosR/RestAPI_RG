using System.Security.Cryptography;
using DotNetEnv;

namespace Seguridad.Hashing {
    public class HashUtils {
        private static readonly int SaltSize;
        private static readonly int KeySize;
        private static readonly int Iterations;

        static HashUtils() {
            string baseDir = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            string envPath = Path.Combine(projectRoot, ".env");

            if (File.Exists(envPath)) {
                Env.Load(envPath);
                Console.WriteLine($".env cargado desde: {envPath}");
            } else {
                Console.WriteLine($".env no encontrado en: {envPath}, usando variables del entorno del sistema.");
            }

            SaltSize = int.Parse(Environment.GetEnvironmentVariable("HASH_SALT_SIZE") ?? "16");
            KeySize = int.Parse(Environment.GetEnvironmentVariable("HASH_KEY_SIZE") ?? "32");
            Iterations = int.Parse(Environment.GetEnvironmentVariable("HASH_ITERATIONS") ?? "100000");
        }


        public static string GenerarHash(string password) {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
            byte[] key = pbkdf2.GetBytes(KeySize);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }

        public static bool ValidarHash(string password, string hashAlmacenado) {
            var partes = hashAlmacenado.Split(':');
            if (partes.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(partes[0]);
            byte[] keyOriginal = Convert.FromBase64String(partes[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
            byte[] keyComparacion = pbkdf2.GetBytes(KeySize);

            return keyComparacion.SequenceEqual(keyOriginal);
        }
    }
}
