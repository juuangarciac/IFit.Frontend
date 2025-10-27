using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFit.Models;
using IFit.Models.Dtos;

namespace IFit.Services
{
    public class AppUserService
    {
        private CoachModelTypeService? CoachModelTypeService = App.GetService<CoachModelTypeService>();
        public AppUserService()  { }

        public async Task<AppUser?> findUserById(long id) 
        {
            var urlAddress = AppSettings.BaseAddress + "/appuser/findById?id=" + id;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if(!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppUser>(responseData);
                }
            }
            return null;
        }

        public async Task<AppUser?> findUserByEmail(string email) 
        {
            var urlAddress = AppSettings.BaseAddress + "/appuser/findByEmail?email=" + Uri.EscapeDataString(email);
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if(!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppUser>(responseData);
                }
            }
            return null;
        }

        public async Task<CoachModelType?> GetSelectedCoachModelTypeByEmail(string email)
        {
            AppUser? appUser = await this.findUserByEmail(email);

            if (appUser != null && AppUser.isPresent(appUser))
            {
                if (CoachModelTypeService != null && appUser.CoachModelTypeId > 0)
                {
                    return await CoachModelTypeService.GetCoachModelTypeById(appUser.CoachModelTypeId.ToString());
                }
            }
            return null;
        }

        public async Task<AppUser?> SetCoachModelType(long? userId, long? coachId)
        {
            if (userId == null || coachId == null || userId <= 0 || coachId <= 0)
            {
                Debug.WriteLine("User ID or Coach ID is invalid");
                return null;
            }

            var urlAddress = AppSettings.BaseAddress + "/appuser/setCoachModelType?userId=" + userId + "&coachId=" + coachId;
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await AppSettings._HttpClient.PostAsync(urlAddress, content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<AppUser>(responseData);
            }

            return null;
        }

        public async Task<AppUser?> SetExperienceLevel(long? userId, long? experienceLevel)
        {
            if (userId == null || experienceLevel == null || userId <= 0 || experienceLevel <= 0)
            {
                Debug.WriteLine("User ID or Coach ID is invalid");
                return null;
            }

            var urlAddress = AppSettings.BaseAddress + "/appuser/setExperienceLevel?userId=" + userId + "&experienceLevelId=" + experienceLevel;
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await AppSettings._HttpClient.PostAsync(urlAddress, content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<AppUser>(responseData);
            }

            return null;
        }

        public async Task<AppQuestionnaire?> GetQuestionnaireForUserAsync(AppUser user)
        {
            if (user.Id <= 0)
            {
                Debug.WriteLine("User ID is invalid");
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/questionnaire/findByExperienceLevelAndCoachModelType?experienceLevelId=" + user.ExperienceLevelId + "&coachModelTypeId=" + user.CoachModelTypeId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppQuestionnaire>(responseData);
                }
            }
            return null;
        }

        public async Task<AppUser?> MarkRegistrationComplete(long? userId)
        {
            if (userId == null || userId <= 0)
            {
                Debug.WriteLine("User ID is invalid");
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/appuser/markRegistrationComplete?userId=" + userId;
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await AppSettings._HttpClient.PostAsync(urlAddress, content);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<AppUser>(responseData);
            }
            return null;
        }
    }
}
