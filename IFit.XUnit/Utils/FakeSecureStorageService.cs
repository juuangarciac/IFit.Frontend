using IFit.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.XUnit.Utils
{
    public class FakeSecureStorageService : ISecureStorageService
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task SetAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            return Task.FromResult(_storage.ContainsKey(key) ? _storage[key] : null);
        }

        public bool Remove(string key) => _storage.Remove(key);
        public void RemoveAll() => _storage.Clear();
    }
}
