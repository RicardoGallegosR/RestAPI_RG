using Microsoft.AspNetCore.Authentication;
using Seguridad.Autenticacion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using SEDEMA_REST_API.BDD;
using System.Net;

System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<Seguridad.Configuracion.Conexion>();
builder.Services.AddTransient<Seguridad.Servicios.UsuarioValidador>();
builder.Services.AddControllers();
builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, Seguridad.Autenticacion.BasicAuthenticationHandler>("Basic", null);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SEDEMA_REST_API.BDD.Conexion>(); // <- desde la biblioteca de Seguridad
builder.Services.AddScoped<spSivev>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
