var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient(); // Registers IHttpClientFactory for making HTTP requests

// Configure CORS to allow requests from the React frontend (e.g., http://localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policyBuilder => policyBuilder.WithOrigins("http://localhost:3000") // Replace with your frontend URL
                                       .AllowAnyMethod()
                                       .AllowAnyHeader());
});

// Swagger configuration for API documentation (optional, useful for development/testing)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline for development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS with the specified policy
app.UseCors("AllowReactApp");

// Enable routing for controller-based endpoints
app.UseRouting();

// Map controllers for API routing
app.MapControllers();

app.Run();
