using System;

namespace ConsoleCSF.Models
{
    public class Proposta
    {
        public long NumeroProposta { get; private set; }
        public DateTime DataHoraCriacao { get; private set; }
        public string DescritivoStatus { get; private set; }
        public int StatusProposta { get; private set; }

        public Proposta(long numeroPropostas, DateTime dataHoraCriacao, string descritivoStatus, int statusProposta)
        {
            NumeroProposta = numeroPropostas;
            DataHoraCriacao = dataHoraCriacao;
            DescritivoStatus = descritivoStatus;
            StatusProposta = statusProposta;
        }
    }
    //TODO: FILTRO PARA ESSES STATUS
    //16, 9, 11, 18, 33, 28, 30
    public enum StatusPropostas
    {
        PropostaDadosCompletosEnviada = 16,
        PropostaDadosMinimosEnviada = 9,
        PropostaDadosMinimosFluxoInterrompido = 11,
        PropostaDadosCompletosFluxoInterrompido = 18,
        PropostaOfertaEnviada = 33,
        PropostaOfertaFluxoInterrompido = 28,
        PropostaPendenteCriacaoDeContaNaProcessadora = 30
    }
}
