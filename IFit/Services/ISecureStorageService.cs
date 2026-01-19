namespace IFit.Services
{
    /// <summary>
    /// Interfaz para abstraer el acceso a SecureStorage de MAUI
    /// Permite mockear en pruebas unitarias
    /// </summary>
    public interface ISecureStorageService
    {
        /// <summary>
        /// Guarda un valor de forma segura
        /// </summary>
        Task SetAsync(string key, string value);

        /// <summary>
        /// Obtiene un valor guardado
        /// </summary>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Elimina un valor
        /// </summary>
        bool Remove(string key);

        /// <summary>
        /// Elimina todos los valores
        /// </summary>
        void RemoveAll();
    }

    /// <summary>
    /// Implementación real de SecureStorage para producción
    /// </summary>
    public class SecureStorageService : ISecureStorageService
    {
        public async Task SetAsync(string key, string value)
        {
            await SecureStorage.Default.SetAsync(key, value);
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                return await SecureStorage.Default.GetAsync(key);
            }
            catch
            {
                return null;
            }
        }

        public bool Remove(string key)
        {
            return SecureStorage.Default.Remove(key);
        }

        public void RemoveAll()
        {
            SecureStorage.Default.RemoveAll();
        }
    }
}