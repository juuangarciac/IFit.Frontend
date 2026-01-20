using IFit.Models.Dtos;
using IFit.Models.Dtos.AppUser;
using IFit.Models.Dtos.AppUser.IFit.Models.Dtos.User;
using IFit.Services;
using IFit.XUnit.Utils;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IFit.XUnit.Unit.Services
{
    public class AppUserServiceTest
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly FakeSecureStorageService _fakeSecureStorage;
        private readonly TokenManager _tokenManager;
        private readonly WebService _webService;
        private readonly AppUserService _userService;

        private const string BASE_URL = "http://localhost:8080/api/v1";
        private const long TEST_USER_ID = 1;
        private const string TEST_EMAIL = "test@example.com";

        public AppUserServiceTest()
        {
            // Setup HttpMessageHandler mock
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(BASE_URL)
            };

            // Setup dependencies
            _fakeSecureStorage = new FakeSecureStorageService();
            _tokenManager = new TokenManager(_fakeSecureStorage);
            _webService = new WebService(_httpClient, _tokenManager, BASE_URL, "/auth/refresh");
            _userService = new AppUserService(_webService);

            // Setup default auth tokens for authenticated requests
            SetupAuthTokens();
        }

        #region Helper Methods

        private void SetupAuthTokens()
        {
            _fakeSecureStorage.SetAsync("ifit_access_token", "valid_access_token").Wait();
            _fakeSecureStorage.SetAsync("ifit_refresh_token", "valid_refresh_token").Wait();
            _fakeSecureStorage.SetAsync("ifit_token_expiry", DateTime.UtcNow.AddHours(1).ToString("o")).Wait();
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, object responseBody)
        {
            var json = JsonSerializer.Serialize(responseBody);
            var response = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);
        }

        private AppUserResponseDto CreateMockUser(long id = TEST_USER_ID, string email = TEST_EMAIL)
        {
            return new AppUserResponseDto
            {
                Id = id,
                Name = "Test User",
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                RoleName = "ROLE_USER",
                CoachModelTypeName = null,
                ExperienceLevelName = null,
                RegistrationComplete = false,
                Verified = false
            };
        }

        #endregion

        #region GetUserById Tests

        [Fact]
        public async Task FindUserById_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var expectedUser = CreateMockUser();
            SetupHttpResponse(HttpStatusCode.OK, expectedUser);

            // Act
            var result = await _userService.findUserById(TEST_USER_ID);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Id, result.Id);
            Assert.Equal(expectedUser.Email, result.Email);
            Assert.Equal(expectedUser.Name, result.Name);
        }

        [Fact]
        public async Task FindUserById_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("{\"message\":\"User not found\"}")
                });

            // Act
            var result = await _userService.findUserById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserById_WithZeroId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.findUserById(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserById_WithNegativeId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.findUserById(-1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUserByEmail Tests

        [Fact]
        public async Task FindUserByEmail_WithValidEmail_ShouldReturnUser()
        {
            // Arrange
            var expectedUser = CreateMockUser();
            SetupHttpResponse(HttpStatusCode.OK, expectedUser);

            // Act
            var result = await _userService.findUserByEmail(TEST_EMAIL);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Email, result.Email);
            Assert.Equal(expectedUser.Name, result.Name);
        }

        [Fact]
        public async Task FindUserByEmail_WithNonExistentEmail_ShouldReturnNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, new { message = "Email not found" });

            // Act
            var result = await _userService.findUserByEmail("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserByEmail_WithEmptyEmail_ShouldReturnNull()
        {
            // Act
            var result = await _userService.findUserByEmail("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserByEmail_WithNullEmail_ShouldReturnNull()
        {
            // Act
            var result = await _userService.findUserByEmail(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserByEmail_WithEmailContainingSpecialChars_ShouldEscapeCorrectly()
        {
            // Arrange
            var specialEmail = "test+special@example.com";
            var expectedUser = CreateMockUser(email: specialEmail);
            SetupHttpResponse(HttpStatusCode.OK, expectedUser);

            // Act
            var result = await _userService.findUserByEmail(specialEmail);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(specialEmail, result.Email);
        }

        #endregion

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsers_ShouldReturnListOfUsers()
        {
            // Arrange
            var expectedUsers = new List<AppUserResponseDto>
            {
                CreateMockUser(1, "user1@example.com"),
                CreateMockUser(2, "user2@example.com"),
                CreateMockUser(3, "user3@example.com")
            };
            SetupHttpResponse(HttpStatusCode.OK, expectedUsers);

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("user1@example.com", result[0].Email);
        }

        [Fact]
        public async Task GetAllUsers_WhenEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, new List<AppUserResponseDto>());

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region SetCoachModelType Tests

        [Fact]
        public async Task SetCoachModelType_WithValidIds_ShouldReturnUpdatedUser()
        {
            // Arrange
            var updatedUser = CreateMockUser();
            updatedUser.CoachModelTypeName = "Ronnie";
            SetupHttpResponse(HttpStatusCode.OK, updatedUser);

            // Act
            var result = await _userService.SetCoachModelType(TEST_USER_ID, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Ronnie", result.CoachModelTypeName);
        }

        [Fact]
        public async Task SetCoachModelType_WithNullUserId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.SetCoachModelType(null, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetCoachModelType_WithNullCoachId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.SetCoachModelType(TEST_USER_ID, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetCoachModelType_WithZeroUserId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.SetCoachModelType(0, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetCoachModelType_WithNegativeCoachId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.SetCoachModelType(TEST_USER_ID, -1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetExperienceLevel Tests

        [Fact]
        public async Task SetExperienceLevel_WithValidIds_ShouldReturnUpdatedUser()
        {
            // Arrange
            var updatedUser = CreateMockUser();
            updatedUser.ExperienceLevelName = "Principiante";
            SetupHttpResponse(HttpStatusCode.OK, updatedUser);

            // Act
            var result = await _userService.SetExperienceLevel(TEST_USER_ID, 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Principiante", result.ExperienceLevelName);
        }

        [Fact]
        public async Task SetExperienceLevel_WithNullUserId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.SetExperienceLevel(null, 2);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetExperienceLevel_WithNullLevelId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.SetExperienceLevel(TEST_USER_ID, null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region MarkRegistrationComplete Tests

        [Fact]
        public async Task MarkRegistrationComplete_WithValidId_ShouldReturnUpdatedUser()
        {
            // Arrange
            var updatedUser = CreateMockUser();
            updatedUser.RegistrationComplete = true;
            SetupHttpResponse(HttpStatusCode.OK, updatedUser);

            // Act
            var result = await _userService.MarkRegistrationComplete(TEST_USER_ID);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RegistrationComplete);
        }

        [Fact]
        public async Task MarkRegistrationComplete_WithNullId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.MarkRegistrationComplete(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MarkRegistrationComplete_WithZeroId_ShouldReturnNull()
        {
            // Act
            var result = await _userService.MarkRegistrationComplete(0);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUser_WithValidDto_ShouldReturnCreatedUser()
        {
            // Arrange
            var createDto = new CreateAppUserRequestDto
            {
                Name = "New User",
                Email = "newuser@example.com",
                Password = "password123"
            };
            var createdUser = CreateMockUser(10, createDto.Email);
            createdUser.Name = createDto.Name;
            SetupHttpResponse(HttpStatusCode.Created, createdUser);

            // Act
            var result = await _userService.CreateUser(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Email, result.Email);
            Assert.Equal(createDto.Name, result.Name);
        }

        [Fact]
        public async Task CreateUser_WithNullDto_ShouldReturnNull()
        {
            // Act
            var result = await _userService.CreateUser(null!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_WithValidDto_ShouldReturnUpdatedUser()
        {
            // Arrange
            var updateDto = new UpdateAppUserRequestDto
            {
                Name = "Updated Name",
                Email = "updated@example.com"
            };
            var updatedUser = CreateMockUser();
            updatedUser.Name = updateDto.Name;
            updatedUser.Email = updateDto.Email!;
            SetupHttpResponse(HttpStatusCode.OK, updatedUser);

            // Act
            var result = await _userService.UpdateUser(TEST_USER_ID, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.Email, result.Email);
        }

        [Fact]
        public async Task UpdateUser_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var updateDto = new UpdateAppUserRequestDto { Name = "Test" };

            // Act
            var result = await _userService.UpdateUser(0, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUser_WithNullDto_ShouldReturnNull()
        {
            // Act
            var result = await _userService.UpdateUser(TEST_USER_ID, null!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NoContent, new { });

            // Act
            var result = await _userService.DeleteUser(TEST_USER_ID);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteUser_WithInvalidId_ShouldReturnFalse()
        {
            // Act
            var result = await _userService.DeleteUser(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUser_WhenUserNotFound_ShouldReturnFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, new { message = "User not found" });

            // Act
            var result = await _userService.DeleteUser(999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region EmailExists Tests

        [Fact]
        public async Task EmailExists_WithExistingEmail_ShouldReturnTrue()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, true);

            // Act
            var result = await _userService.EmailExists(TEST_EMAIL);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EmailExists_WithNonExistentEmail_ShouldReturnFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, false);

            // Act
            var result = await _userService.EmailExists("nonexistent@example.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EmailExists_WithEmptyEmail_ShouldReturnFalse()
        {
            // Act
            var result = await _userService.EmailExists("");

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}