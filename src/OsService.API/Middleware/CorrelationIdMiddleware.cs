using Serilog.Context;

namespace OsService.API.Middleware;

/// <summary>
/// Gera (ou propaga, se já vier de outro serviço) um X-Correlation-Id por
/// requisição e o injeta no contexto de log estruturado do Serilog. Mesmo
/// padrão usado na Fase 3 do monólito — aqui ganha importância extra: como a
/// OrdemServico dispara eventos consumidos por outros microsserviços (Billing,
/// Execução), o CorrelationId (igual ao Id da OS) permite rastrear a saga
/// inteira correlacionando logs entre serviços distintos.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
