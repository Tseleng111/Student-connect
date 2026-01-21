using FirebaseAuth.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http; // <-- added
using System.Net.Mail;
using Firebase.Database;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args); // <-- builder must come first

// ==================== FIREBASE ADMIN SDK ====================
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(
        Path.Combine(builder.Environment.ContentRootPath, "Storage", "studentconnect-693f9-firebase-adminsdk-fbsvc-9720313817.json")
    )
});

// ==================== SERVICES ====================

// Allow controllers to access HttpContext
builder.Services.AddHttpContextAccessor();

// Register FirebaseClient for Realtime Database
builder.Services.AddSingleton(new FirebaseClient("https://studentconnect-693f9-default-rtdb.firebaseio.com/"));

// Register Student Photo service for DI
builder.Services.AddScoped<IStudentPhotoService, FirebaseStudentPhotoService>();

// Register HttpClient so FirebaseStudentService can be constructed
builder.Services.AddHttpClient(); // <-- NEW

// Register FirebaseStudentService for DI
builder.Services.AddScoped<FirebaseStudentService>();

// ======== REGISTER EMAIL SERVICE ========
builder.Services.AddScoped<EmailService>();  // <-- THIS IS NEW

// Add controllers with views
builder.Services.AddControllersWithViews();
//builder.Services.Configure<SmtpSettings>(
//    builder.Configuration.GetSection("SmtpSettings"));
//builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// your FirebaseService already registered here if you have one

// ==================== SESSION ====================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==================== AUTHENTICATION ====================
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

// ==================== AUTHORIZATION ====================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== MIDDLEWARE ====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ==================== DEFAULT ROUTE ====================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
