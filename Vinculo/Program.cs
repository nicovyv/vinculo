using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using Vinculo.Data;
using Vinculo.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// incluir DbContext
builder.Services.AddDbContext<VinculoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VinculoDbContext")));

//Registrar Identity y vincularlo con tu DbContext
builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    // Opciones opcionales para simplificar las pruebas mientras desarrollan
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<VinculoDbContext>()
.AddDefaultTokenProviders();

//ruta login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Usuario/Login";
    options.LogoutPath = "/Usuario/Logout";
    options.AccessDeniedPath = "/Usuario/Login";
});

var app = builder.Build();


// Ejecutar el Seeder al arrancar la aplicación
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Llamamos al método que creamos
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        // En un entorno real aquí se loguea el error
        Console.WriteLine($"Ocurrió un error al sembrar la base de datos: {ex.Message}");
    }
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuario}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
