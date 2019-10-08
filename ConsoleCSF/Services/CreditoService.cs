using ConsoleCSF.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Xml;

namespace ConsoleCSF.Services
{
    public static class CreditoService
    {
        public static void SolucionarProblemasPropostas()
        {
            Console.WriteLine("Inicializando Aplicação!");
            Console.WriteLine("=======================");
            List<Proposta> propostas = BuscarPropostas();
            if (propostas.Count > 0)
            {
                string token = AutenticacaoConsumidor();
                if (string.IsNullOrEmpty(token))
                {
                    Console.Clear();
                    Console.WriteLine("Tentando novamente...");
                    Console.WriteLine("");
                    SolucionarProblemasPropostas();
                }
                else
                {
                    foreach (var item in propostas)
                    {
                        EnviarProposta(item, token);
                    }
                }
            }
        }

        private static List<Proposta> BuscarPropostas()
        {
            List<Proposta> propostas = new List<Proposta>();
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = ConfigurationManager.AppSettings["ipBD"];
                builder.UserID = ConfigurationManager.AppSettings["usuarioBD"];
                builder.Password = ConfigurationManager.AppSettings["senhaBD"];
                builder.InitialCatalog = ConfigurationManager.AppSettings["inicialBD"];

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    Console.WriteLine("Conectando ao Banco.");
                    Console.WriteLine("=======================");
                    connection.Open();
                    Console.WriteLine("Conectado com sucesso.");
                    Console.WriteLine("=======================");
                    SqlCommand command = connection.CreateCommand();

                    command.CommandText = @"use credito
        select p.NU_PROP, P.DH_CRIAC, d.DS_VLDOMIN, p.CD_STATUSPROP
        from credito.dbo.TB_Proposta P
        Inner Join credito.dbo.TB_TITULAR_DETALHE T on  p.ID_TITDET = T.ID_TITDET
        Inner Join corporativo.dbo.TB_VALOR_DOMINIO D on CONVERT(varchar,p.CD_STATUSPROP) = d.CD_VLDOMIN
        Left Join pr..TB_PROCESD_CONTROLE pd on pd.NU_PROPTIT = p.NU_PROP
        where (p.CD_STATUSPROP in (" + ConfigurationManager.AppSettings["filtroStatus"] + ") " +
        "and p.DH_ALT <= DATEADD(SECOND,-180,GETDATE()) and d.CD_TPDOMIN = 4) and p.DH_Criac >= '" + ConfigurationManager.AppSettings["dataInicioConsulta"] + "' " +
        "order by p.DH_CRIAC ";

                    Console.WriteLine("Consultando propostas...");
                    Console.WriteLine("=======================");
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        Console.WriteLine($"Consulta Finalizada, construindo objeto.");
                        Console.WriteLine("=======================");
                        while (reader.Read())
                        {
                            propostas.Add(
                                new Proposta(
                                    Convert.ToInt64(reader["NU_PROP"]),
                                    Convert.ToDateTime(reader["DH_CRIAC"]),
                                    reader["DS_VLDOMIN"].ToString(),
                                    Convert.ToInt32(reader["CD_STATUSPROP"]))
                                );
                        }
                    }
                    Console.WriteLine($"Encontradas {propostas.Count} propostas.");
                    Console.WriteLine("=======================");
                }
                return propostas;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocorreu um erro ao realizar a consulta.");
                Console.WriteLine($"Mensagem: {ex.Message}");
                return propostas;
            }
        }

        private static string AutenticacaoConsumidor()
        {
            Console.WriteLine("Autenticando.");
            Console.WriteLine("=======================");
            RestClient cliente = new RestClient(ConfigurationManager.AppSettings["urlPostAutenticacao"]);
            RestRequest request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Connection", "Keep-Alive");
            request.AddHeader("Host", ConfigurationManager.AppSettings["ipHostAutenticacaoSoap"]);
            request.AddHeader("SOAPAction", ConfigurationManager.AppSettings["soapActionAutenticacao"]);
            request.AddHeader("Content-Type", "text/xml;charset=UTF-8");
            request.AddParameter("undefined", "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\" xmlns:car=\"http://schemas.datacontract.org/2004/07/Carrefour.Core.Servico.ComunicacaoBase\" xmlns:car1=\"http://schemas.datacontract.org/2004/07/Carrefour.Core.Servico.Validacao\" xmlns:car2=\"http://schemas.datacontract.org/2004/07/Carrefour.Servico.Autenticacao.Entidade\">\r\n\r\n   <soapenv:Header/>\r\n\r\n   <soapenv:Body>\r\n\r\n      <tem:AutenticarConsumidor>\r\n\r\n         <tem:solicitacao>\r\n\r\n            <car:CanalSolicitacao>" + ConfigurationManager.AppSettings["canalSolicitacao"] + "</car:CanalSolicitacao>\r\n\r\n            <car:ChaveSolicitacao>" + ConfigurationManager.AppSettings["chaveSolicitacaoLogin"] + "</car:ChaveSolicitacao>\r\n\r\n            <car2:CodigoHierarquia>" + ConfigurationManager.AppSettings["codigoHierarquia"] + "</car2:CodigoHierarquia>\r\n\r\n            <car2:Senha>53rvC@r3#2013</car2:Senha>\r\n\r\n            <car2:Usuario>SERVCORE</car2:Usuario>\r\n\r\n         </tem:solicitacao>\r\n\r\n      </tem:AutenticarConsumidor>\r\n\r\n   </soapenv:Body>\r\n\r\n</soapenv:Envelope>", ParameterType.RequestBody);
            IRestResponse resposta = cliente.Execute(request);

            if (resposta.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Autenticado com sucesso.");
                Console.WriteLine("=======================");
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resposta.Content);
                var objeto = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeXmlNode(doc));
                string token = objeto["s:Envelope"]["s:Body"]["AutenticarConsumidorResponse"]["AutenticarConsumidorResult"]["a:Token"];
                return token;
            }
            else
            {
                Console.WriteLine($"Requisição retornou o código {resposta.StatusCode} com a descrição {resposta.StatusDescription}");
                Console.WriteLine("=======================");
                return string.Empty;
            }
        }

        private static void EnviarProposta(Proposta proposta, string token)
        {
            Console.WriteLine($"Enviando proposta com numero {proposta.NumeroProposta}.");
            Console.WriteLine("=======================");
            var cliente = new RestClient(ConfigurationManager.AppSettings["enviarPropostaPostUrl"]);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Connection", "Keep-Alive");
            request.AddHeader("Host", ConfigurationManager.AppSettings["ipHostEmviarPropostaSoap"]);
            request.AddHeader("SOAPAction", ConfigurationManager.AppSettings["soapActionEnviarProposta"]);
            request.AddHeader("Content-Type", "text/xml;charset=UTF-8");
            request.AddParameter("undefined", "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/' xmlns:csf='http://schemas.datacontract.org/2004/07/Csf.Core.Base.Service.Communication' xmlns:csf1='http://schemas.datacontract.org/2004/07/Csf.Core.Base.Service.Validation' xmlns:csf2='http://schemas.datacontract.org/2004/07/Csf.PR.Svc.PropostaCartao.Contrato.Pacote.GerenciadorProposta'><soapenv:Header>    <TokenPlataformaRelacionamento>" + token + "</TokenPlataformaRelacionamento></soapenv:Header><soapenv:Body><tem:SolucionarProblemasProposta><tem:solicitacao><csf:CanalSolicitacao>" + ConfigurationManager.AppSettings["canalSolicitacao"] + "</csf:CanalSolicitacao><csf:ChaveSolicitacao>" + ConfigurationManager.AppSettings["chaveSolicitacaoRequisicao"] + "</csf:ChaveSolicitacao><csf2:NumeroProposta>" + proposta.NumeroProposta + "</csf2:NumeroProposta></tem:solicitacao></tem:SolucionarProblemasProposta></soapenv:Body></soapenv:Envelope>", ParameterType.RequestBody);
            IRestResponse resposta = cliente.Execute(request);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(resposta.Content);
            dynamic objeto = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeXmlNode(doc));
            bool sucesso = (bool)objeto["s:Envelope"]["s:Body"]["SolucionarProblemasPropostaResponse"]["SolucionarProblemasPropostaResult"]["Resultado"]["b:Sucesso"];
            string erro = objeto["s:Envelope"]["s:Body"]["SolucionarProblemasPropostaResponse"]["SolucionarProblemasPropostaResult"]["Resultado"]["b:Descricao"].ToString();

            if (sucesso)
            {
                Console.WriteLine($"Proposta {proposta.NumeroProposta} enviada com sucesso.");
                Console.WriteLine("=======================");
            }
            else
            {
                Console.WriteLine($"Proposta {proposta.NumeroProposta} enviada com erro.");
                Console.WriteLine($"Mensagem: {erro}");
                Console.WriteLine("=======================");
            }
        }
    }
}
