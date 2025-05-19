using MicroserviciosRepoEscom.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MicroserviciosRepoEscom.Servicios
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;
        private readonly List<string> _adminEmails = new List<string>
        {
            "aavilaj1701@alumno.ipn.mx",
            "ajurados1600@alumno.ipn.mx",
            "agutierrezv1500@alumno.ipn.mx"
            // Agregar los correos de los administradores aquí
        };

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("ResendApi");
            _httpClient.BaseAddress = new Uri("https://api.resend.com");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _emailSettings.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> SendEmailAsync(string nombreMaterial, string autorNombre)
        {
            try
            {
                _logger.LogInformation($"Enviando notificación de material ZIP: {nombreMaterial}");

                string subject = "Nueva petición de material ZIP - Repositorio ESCOM";
                string message = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #2c3e50; color: white; padding: 20px; text-align: center;'>
                        <h1>📦 Nueva Petición de Material</h1>
                        <p>Repositorio Digital ESCOM</p>
                    </div>
                    
                    <div style='padding: 20px; background-color: #f8f9fa;'>
                        <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                            <p style='color: #856404; margin: 0;'>
                                <strong>⚠️ Acción requerida:</strong> Se ha subido un archivo ZIP que necesita revisión y aprobación.
                            </p>
                        </div>
                        
                        <p><strong>Nombre del material:</strong> {nombreMaterial}</p>
                        <p><strong>Autor(es):</strong> {autorNombre}</p>
                        <p><strong>Fecha de subida:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm}</p>
                        <p><strong>Estado:</strong> <span style='color: #dc3545;'>🔒 Deshabilitado (Pendiente de revisión)</span></p>
                        
                        <div style='margin-top: 20px; padding: 15px; background-color: white; border-radius: 5px;'>
                            <p style='margin: 0;'>Por favor, revise el material y habilítelo si cumple con los criterios del repositorio.</p>
                        </div>
                    </div>
                    
                    <div style='text-align: center; padding: 20px; color: #6c757d; font-size: 12px;'>
                        <p>Este es un mensaje automático del Repositorio Digital ESCOM</p>
                        <p>No responda a este correo</p>
                    </div>
                </div>";

                // Crear payload para múltiples destinatarios
                var emailRequest = new
                {
                    from = $"{_emailSettings.DisplayName} <{_emailSettings.Mail}>",
                    to = _adminEmails.ToArray(),
                    subject = subject,
                    html = message
                };

                var json = JsonSerializer.Serialize(emailRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _httpClient.PostAsync("/emails", content, cts.Token);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Notificación ZIP - Código: {(int)response.StatusCode}, Respuesta: {responseBody}");

                return response.IsSuccessStatusCode;
            }
            catch(TaskCanceledException ex)
            {
                _logger.LogError($"Timeout al enviar correo: {ex.Message}");
                return false;
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error al enviar correo: {ex.GetType().Name} - {ex.Message}");
                if(ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }


        }
    }
}
