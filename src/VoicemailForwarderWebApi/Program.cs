using System.Reflection;
using SendGrid.Extensions.DependencyInjection;
using Twilio.AspNet.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true, false);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTwilioClient((serviceProvider, options) =>
{
    options.AccountSid = builder.Configuration["TwilioSettings:AccountSid"]; 
    options.AuthToken = builder.Configuration["TwilioSettings:AuthToken"];
});
builder.Services.AddHttpClient();
builder.Services.AddSendGrid(options => options.ApiKey = builder.Configuration["SendGridSettings:ApiKey"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
