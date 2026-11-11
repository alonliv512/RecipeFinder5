using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Model;
 

namespace DAL
{
    public class UserDB
    {
        private readonly string connectionString;

        public UserDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        private User ReadUser(System.Data.Common.DbDataReader reader)
        {
            return new User(
                Convert.ToInt32(reader["UserID"]),
                reader["Username"].ToString(),
                reader["PasswordHash"].ToString(),
                Convert.ToInt32(reader["RoleID"]),
                reader["RoleName"].ToString(),
                Convert.ToBoolean(reader["IsActive"]),
                reader["Email"] != DBNull.Value ? reader["Email"].ToString() : string.Empty // הוספת שדה האימייל
            );
        }

        public async Task<int> AddUserAsync(User user)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO Users (Username, PasswordHash, RoleID, IsActive, Email) 
                             VALUES (@Username, @PasswordHash, @RoleID, @IsActive, @Email);
                             SELECT LAST_INSERT_ID();";

            // יצירת גיבוב מאובטח לסיסמה הרגילה שהמשתמש הקליד
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", hashedPassword); // שולחים את הגיבוב ולא את הסיסמה הגלויה
            command.Parameters.AddWithValue("@RoleID", user.RoleID == 0 ? 2 : user.RoleID);
            command.Parameters.AddWithValue("@IsActive", user.IsActive);
            command.Parameters.AddWithValue("@Email", user.Email ?? string.Empty);
            var id = await command.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            long count = (long)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            long count = (long)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            // ודא ששם הפונקציה תואם למה שקיים אצלך ב-DAL (למשל GetByUsernameAsync או דומה)
            User? user = await GetUserByUsernameAsync(username);

            // משתמשים ב-user.Password כי כך נקרא השדה במחלקת User שלך
            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                // BCrypt.Verify משווה את הסיסמה שהוקלדה מול הגיבוב השמור
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (isPasswordValid)
                {
                    return user; // הסיסמה נכונה! מחזירים את אובייקט המשתמש
                }
            }

            return null; // שם משתמש לא קיים או סיסמה שגויה
        }
        public async Task<User> GetUserByIdAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT u.UserID, u.Username, u.PasswordHash, u.RoleID, r.RoleName, u.IsActive, u.Email 
                             FROM Users u
                             JOIN Roles r ON u.RoleID = r.RoleID
                             WHERE u.UserID = @UserID";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserID", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUser(reader);
            }
            return null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT u.UserID, u.Username, u.PasswordHash, u.RoleID, r.RoleName, u.IsActive, u.Email 
                             FROM Users u
                             JOIN Roles r ON u.RoleID = r.RoleID";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(ReadUser(reader));
            }
            return users;
        }

        public async Task<int> UpdateUserRoleAsync(int userId, int newRoleId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE Users SET RoleID = @RoleID WHERE UserID = @UserID";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@RoleID", newRoleId);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdatePasswordAsync(int userId, string newPassword)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // כאן אנחנו מבצעים את הגיבוב (Hash) בעזרת BCrypt בתוך שכבת ה-DAL עצמה!
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            string query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserID = @UserID";
            using var command = new MySqlCommand(query, connection);

            // שולחים למסד הנתונים את הגיבוב המאובטח ולא את הסיסמה הגלויה
            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync();
        }
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT u.UserID, u.Username, u.PasswordHash, u.RoleID, r.RoleName, u.IsActive, u.Email 
                     FROM Users u
                     JOIN Roles r ON u.RoleID = r.RoleID
                     WHERE u.Username = @Username";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUser(reader);
            }
            return null; // אם לא נמצא משתמש בשם הזה
        }

    }
}