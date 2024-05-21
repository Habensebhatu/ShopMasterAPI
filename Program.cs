using System.Text;
using Azure.Storage.Blobs;
using business_logic_layer;
using business_logic_layer.ViewModel;
using Data_layer.Context.Data;
using Data_layer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Stripe;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddDbContext<MyDbContext>(options =>
//        options.UseSqlServer(builder.Configuration.GetConnectionString("ProdConnection")));


builder.Services.AddDbContext<MyDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Aitmaten")));

builder.Services.AddCors(p => p.AddPolicy("corspolicy", build => {

    build.AllowAnyOrigin()
    .AllowAnyMethod().AllowAnyHeader();
}));

builder.Services.AddDbContext<MyDbcontextSofani>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connStr = configuration.GetConnectionString("Sofanimarket");
    options.UseSqlServer(connStr);
});

builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();


builder.Services.AddAuthentication(configureOptions: options =>
{

}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateAudience = false,
        ValidateIssuer = false,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration.GetSection("AppSettings:Token").Value!))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(x => new BlobServiceClient(builder.Configuration.GetConnectionString("AzureblobConnectionString")));
builder.Services.Configure<emailSettingsModel>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<EmailSettingsAitmaten>(builder.Configuration.GetSection("EmailSettingsAitmaten"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailServiceAit, EmailServiceAit>();
builder.Services.AddTransient<IEmailContactUs, ContactUsBLL>();
builder.Services.AddTransient<ISendRegistration, SendRegistration>();
builder.Services.Configure<TemplateConfig>(builder.Configuration.GetSection("TemplatePaths"));
builder.Services.AddSingleton(builder.Configuration); 

builder.Services.ConfigureSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Weather Forecasts",
        Version = "v1"
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSwagger();

app.UseCors("corspolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
