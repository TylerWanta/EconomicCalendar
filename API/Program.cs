using API.Firestore;

// setup validation for firestore
string pathToFirestoreKey = AppDomain.CurrentDomain.BaseDirectory + "firestoreKey.json";
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToFirestoreKey);

// setup Usage Tracker
EconomicEventsDBUsageTracker.SetDate();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
