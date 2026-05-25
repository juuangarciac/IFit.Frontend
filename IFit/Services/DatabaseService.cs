using IFit.Models;
using IFit.Models.Dtos.Exercise;
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
            await _db.CreateTableAsync<ExerciseCacheEntity>();
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
                    Preferences.Set("UserId", appUser.Id);
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
                    Preferences.Set("UserId", appUser.Id);
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

        public async Task DeleteAppUserAsync(AppUser appUser)
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            try
            {
                await _db.DeleteAsync(appUser);
                Preferences.Remove("UserId");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user from database: {ex.Message}");
            }
        }

        public async Task DeleteAllAppUsersAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }

            try
            {
                await _db.DeleteAllAsync<AppUser>();
                Preferences.Remove("UserId");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting all users from database: {ex.Message}");
            }
        }

        #region Exercise cache

        public async Task<int> GetExerciseCountAsync()
        {
            if (!_initialized) await InitializeAsync();
            return await _db.Table<ExerciseCacheEntity>().CountAsync();
        }

        public async Task BulkInsertExercisesAsync(IEnumerable<ExerciseSummaryDto> exercises)
        {
            if (!_initialized) await InitializeAsync();
            var entities = exercises.Select(ExerciseCacheEntity.FromDto).ToList();
            await _db.DeleteAllAsync<ExerciseCacheEntity>();
            await _db.InsertAllAsync(entities);
        }

        public async Task<List<ExerciseSummaryDto>> SearchExercisesAsync(string query, int limit = 8)
        {
            if (!_initialized) await InitializeAsync();
            var entities = await _db.QueryAsync<ExerciseCacheEntity>(
                "SELECT * FROM ExerciseCache WHERE Name LIKE ? LIMIT ?",
                $"%{query}%", limit);
            return entities.Select(e => e.ToDto()).ToList();
        }

        #endregion
    }
}
