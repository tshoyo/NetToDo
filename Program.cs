using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NetToDo.Services;
using NetToDo.Data;
using Microsoft.EntityFrameworkCore;
using NetToDo.Models;
using BCrypt.Net;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? "SuperSecretKeyForDevelopmentOnly123!");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    SeedData(context, app.Environment.ContentRootPath);
}

// Ensure uploads directory exists
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

void SeedData(AppDbContext context, string contentRootPath)
{
    if (context.Users.Any()) return;

    var user = new User
    {
        Name = "Test User",
        Email = "test@example.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
    };
    context.Users.Add(user);
    context.SaveChanges();

    var list = new TodoList
    {
        Name = "General",
        UserId = user.Id
    };
    context.TodoLists.Add(list);

    var workList = new TodoList
    {
        Name = "Work",
        UserId = user.Id
    };
    context.TodoLists.Add(workList);
    context.SaveChanges();

    var now = DateTime.UtcNow;
    var items = new List<TodoItem>
    {
        new TodoItem { Title = "Buy groceries", ListId = list.Id, Position = 1, DueDate = now.AddDays(2) },
        new TodoItem { Title = "Call mom", ListId = list.Id, Position = 2, DueDate = now.AddDays(-1) },
        new TodoItem { Title = "Finish report", ListId = workList.Id, Position = 1, DueDate = now.AddDays(5) },
        new TodoItem { Title = "Email team", ListId = workList.Id, Position = 2, DueDate = now.AddDays(1) }
    };
    context.TodoItems.AddRange(items);
    context.SaveChanges();

    var reportItem = items.First(i => i.Title == "Finish report");
    var uploadsPath = Path.Combine(contentRootPath, "wwwroot", "uploads");
    if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
    
    var logoSource = Path.Combine(uploadsPath, "oasco-logo.png");
    
    if (File.Exists(logoSource))
    {
        var attachment = new Attachment
        {
            FileName = "oasco-logo.png",
            FilePath = logoSource,
            TodoItemId = reportItem.Id
        };
        context.Attachments.Add(attachment);
        context.SaveChanges();
    }
}
