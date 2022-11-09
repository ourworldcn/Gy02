using GuangYuan.GY001.TemplateDb;
using Gy02Bll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

#region ����Swagger
//ע��Swagger������������һ�� Swagger �ĵ�
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = $"��Ԫ02",
        Description = "�ӿ��ĵ�v2.0.0"
    });
    // Ϊ Swagger ����xml�ĵ�ע��·��
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
#endregion ����Swagger

#region �������ݿ�
var loggingDbConnectionString = builder.Configuration.GetConnectionString("LoggingDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var userDbConnectionString = builder.Configuration.GetConnectionString("UserDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);
var templateDbConnectionString = builder.Configuration.GetConnectionString("TemplateDbConnection").Replace("{Env}", builder.Environment.EnvironmentName);

services.AddDbContext<GY02TemplateContext>(options => options.UseLazyLoadingProxies().UseSqlServer(templateDbConnectionString).EnableSensitiveDataLogging(), ServiceLifetime.Singleton);

#endregion �������ݿ�

services.AddHostedService<GameHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())

IWebHostEnvironment env = app.Environment;

#region �����м����������Swagger
app.UseSwagger();
app.UseSwaggerUI();
//�����м����������SwaggerUI��ָ��Swagger JSON�ս��
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", env.EnvironmentName + $" V2");
//    c.RoutePrefix = string.Empty;//���ø��ڵ����
//});
#endregion �����м����������Swagger


//app.UseAuthorization();

app.MapControllers();

app.Run();
