using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NLToSQLApp.Models;
using NLToSQLApp.Services;
using MeetApp.AzureOpenAIHub.DataManager;
using MeetApp.AzureOpenAIHub.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ISqliteSchemaService, SqliteSchemaService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();

// Register SavedAINLQ services
builder.Services.AddScoped<SavedAINLQDataManagerHelper>();
builder.Services.AddScoped<ISavedAINLQService, SavedAINLQService>();
builder.Services.AddScoped<ITokenUsageService, TokenUsageService>(); // Replace with your actual implementation

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();