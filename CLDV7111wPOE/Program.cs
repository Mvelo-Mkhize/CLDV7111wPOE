using CLDV7111wPOE.Data;
using CLDV7111wPOE.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()        
           .LogTo(Console.WriteLine, LogLevel.Information));    

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".EventEase.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddResponseCaching();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCaching();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var created = context.Database.EnsureCreated();
        if (created)
        {
            logger.LogInformation("Database was created.");
        }
        else
        {
            logger.LogInformation("Database already exists.");
        }

        if (context.Database.CanConnect())
        {
            logger.LogInformation("Successfully connected to database.");

            logger.LogInformation("Users count: {count}", context.Users.Count());
            logger.LogInformation("Employees count: {count}", context.Employees.Count());
            logger.LogInformation("Administrators count: {count}", context.Administrators.Count());
            logger.LogInformation("Venues count: {count}", context.Venues.Count());
            logger.LogInformation("Events count: {count}", context.Events.Count());
            logger.LogInformation("Bookings count: {count}", context.Bookings.Count());
            logger.LogInformation("BookingRequests count: {count}", context.BookingRequests.Count());
        }
        else
        {
            logger.LogError("Failed to connect to database!");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while connecting to the database.");
    }
}

app.Run();