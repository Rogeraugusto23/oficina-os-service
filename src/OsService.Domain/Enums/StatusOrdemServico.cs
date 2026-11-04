using System;
using System.Collections.Generic;
using System.Text;

namespace OsService.Domain.Enums;

public enum StatusOrdemServico 
{ 
    Recebida = 1, 
    EmDiagnostico = 2, 
    AguardandoAprovacao = 3, 
    EmExecucao = 4, 
    Finalizada = 5, 
    Entregue = 6,
    Cancelada = 7
}
