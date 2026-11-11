using Microsoft.JSInterop;
using Model;

namespace RecipeFinderView.Services
{
    public class UserSession
    {
        private readonly IJSRuntime _jsRuntime;

        public UserSession(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public string RoleName { get; private set; } = string.Empty;

        public bool IsLoggedIn => UserId > 0;

        public event Action? OnChange;

        // טעינת נתונים שמורים בעת עליית הסשן
        public async Task InitializeAsync()
        {
            try
            {
                var username = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
                if (!string.IsNullOrEmpty(username))
                {
                    Username = username;

                    // המרה בטוחה של ה-ID ממחרוזת למספר
                    var userIdStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userId");
                    if (int.TryParse(userIdStr, out int parsedId))
                    {
                        UserId = parsedId;
                    }
                    else
                    {
                        UserId = 1; // ערך גיבוי אם ההמרה נכשלה אבל יש שם משתמש
                    }

                    Email = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "email") ?? "";
                    NotifyStateChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing session: " + ex.Message);
            }
        }
        public async Task Login(User user)
        {
            UserId = user.Id;
            Username = user.Username;
            Email = user.Email;
            RoleId = user.RoleID;
            RoleName = user.RoleName;

            // שמירה ב-LocalStorage של הדפדפן כדי שיישמר גם אחרי רענון וניווט
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", user.Username);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", user.Id);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "email", user.Email ?? "");

            NotifyStateChanged();
        }

        public async Task Logout()
        {
            UserId = 0;
            Username = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            RoleName = string.Empty;

            // מחיקה מזיכרון הדפדפן
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "email");

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}