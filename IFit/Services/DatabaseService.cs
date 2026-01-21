using IFit.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;
        private Boolean _initialized = false;

        public DatabaseService()
        {
            string dbPath = AppSettings.DatabasePath;
            _db = new SQLiteAsyncConnection(dbPath);            
        }

        public async Task InitializeAsync()
        {
            // Create database tables
            await _db.CreateTableAsync<AppUser>();
            await _db.CreateTableAsync<CoachModelTypeDto>();
            _initialized = true;
        }

        public SQLiteAsyncConnection GetConnection() => _db;

        /// synchronously saves an AppUser to the database. 
        public async Task InsertAppUserAsync(AppUser? appUser)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            if (appUser == null)
            {
                Console.WriteLine("Error: appUser is null.");
                return;
            }
            
            try
            {
                // Check if user already exists by ID
                if (await GetAppUserAsyncById(appUser.Id) == null)
                {
                    await _db.InsertAsync(appUser);
                }
                else
                {
                    await _db.UpdateAsync(appUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user to database: {ex.Message}");
            }
        }

        public async Task SaveAppUserAsync(AppUser? appUser)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            if (appUser == null)
            {
                Console.WriteLine("Error: appUser is null.");
                return;
            }

            try
            {
                // Check if user already exists by ID
                if (await GetAppUserAsyncById(appUser.Id) == null)
                {
                    await _db.InsertAsync(appUser);
                }
                else
                {
                    await _db.UpdateAsync(appUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user to database: {ex.Message}");
            }
        }

        // synchronously retrieves the first AppUser from the database.
        public async Task<AppUser?> GetCurrentUserAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
            var currentUserId = Preferences.Get("UserId",0L);

            return 
                await _db.Table<AppUser>().Where(user => user.Id == currentUserId).FirstOrDefaultAsync() 
                ?? null;
        }

        // synchronously retrieves an AppUser by its ID from the database.
        public async Task<AppUser?> GetAppUserAsyncById(long id)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            return await _db.Table<AppUser>().Where(user => user.Id.Equals(id)).FirstOrDefaultAsync();
        }
    }
}
