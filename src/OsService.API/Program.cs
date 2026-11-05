using Amazon.SimpleNotificationService;
using Amazon.SQS;
using OsService.Application.Saga;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OsService.API.Middleware;
using OsService.Application.Common;
using OsService.Application.UseCases.Clientes;
using OsService.Application.UseCases.OrdensServico;
using OsService.Domain.Interfaces;
using OsService.Infrastructure.Data;
using OsService.Infrastructure.Messaging;
using OsService.Infrastructure.Repositories;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .Enrich.FromLogContext()
              .WriteTo.Console(new CompactJsonFormatter()));

    // 1. Banco de Dados — RDS dedicado ao OS Service (nenhum outro
    //    microsserviço da Fase 4 acessa este banco diretamente; a comunicação
    //    com Billing/Execução é feita exclusivamente via eventos assíncronos).
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=OsServiceDB;Trusted_Connection=True;MultipleActiveResultSets=true";

    builder.Services.AddDbContext<OsServiceDbContext>(options =>
        options.UseSqlServer(connectionString));

    // 1.1 Repositórios (Infrastructure implementa contratos do Domain)
    builder.Services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
    builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

    // 1.2 Mensageria (SNS) — publica eventos de integração da Saga
    builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
    builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();
    builder.Services.AddAWSService<IAmazonSQS>();
    builder.Services.AddScoped<SagaOrchestrator>();
    builder.Services.AddHostedService<SqsEventConsumerBackgroundService>();

    // 1.3 Use Cases — módulo OrdensServico
    builder.Services.AddScoped<AbrirOrdemServicoUseCase>();
    builder.Services.AddScoped<ListarOrdensServicoUseCase>();
    builder.Services.AddScoped<ConsultarOrdemServicoUseCase>();
    builder.Services.AddScoped<AvancarStatusUseCase>();
    builder.Services.AddScoped<EnviarOrcamentoUseCase>();
    builder.Services.AddScoped<AprovarOrcamentoUseCase>();
    builder.Services.AddScoped<RejeitarOrcamentoUseCase>();
    builder.Services.AddScoped<AtualizarStatusPorEmailUseCase>();

    // 1.4 Use Cases — módulo Clientes
    builder.Services.AddScoped<CriarClienteUseCase>();
    builder.Services.AddScoped<ListarClientesUseCase>();
    builder.Services.AddScoped<ConsultarClienteUseCase>();
    builder.Services.AddScoped<InativarClienteUseCase>();
    builder.Services.AddScoped<ReativarClienteUseCase>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // 2. Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "OS Service - Oficina Wagyu (Fase 4)", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\""
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // 3. JWT — mesmo segredo compartilhado (Jwt:Secret) usado pela Lambda de
    //    autenticação via CPF (Fase 3) e por Billing/Execução Service, para que
    //    um único token sirva em todos os microsserviços.
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ChaveSecretaOficinaWagyu2026_MuitoLonga";
    var key = Encoding.ASCII.GetBytes(jwtSecret);
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OsServiceDbContext>();
        db.Database.Migrate();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Healthcheck simples, usado pelos probes do Kubernetes (readiness/liveness)
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "os-service", timestamp = DateTime.UtcNow }))
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OS Service terminou de forma inesperada durante a inicialização");
}
finally
{
    Log.CloseAndFlush();
}
