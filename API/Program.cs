using DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 1. שליפת מחרוזת החיבור הבסיסית ובדיקה שהיא קיימת
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in configuration.");
}

// 2. אתחול בסיס הנתונים והטבלאות בהזנקה מול המחרוזת הבסיסית (ללא Database=...)
await DatabaseInitializer.InitializeAsync(connectionString);

// 3. יצירת מחרוזת החיבור המלאה הכוללת את שם מסד הנתונים עבור השרת
var fullConnectionString = connectionString + ";Database=RecipeFinderDB;";

// 4. הוספת ה-UserDB עם המחרוזת המלאה
builder.Services.AddScoped<UserDB>(provider => new UserDB(fullConnectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
