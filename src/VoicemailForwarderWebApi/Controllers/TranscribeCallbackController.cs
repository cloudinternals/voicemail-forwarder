using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio.AspNet.Core;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account.Recording;

namespace TwilioWebhookLambda.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TranscribeCallbackController : TwilioController
{
    private readonly ILogger<TranscribeCallbackController> _logger;
    private readonly ITwilioRestClient _twilioClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISendGridClient _sendGridClient;
    
    public TranscribeCallbackController(ILogger<TranscribeCallbackController> logger, ITwilioRestClient twilioClient, IHttpClientFactory httpClientFactory, ISendGridClient sendGridClient)
    {
        _logger = logger;
        _twilioClient = twilioClient;
        _httpClientFactory = httpClientFactory;
        _sendGridClient = sendGridClient;
    }

    [HttpPost]
    public async Task Index()
    {
        var form = await Request.ReadFormAsync();
        var recordingSid = form["RecordingSid"].ToString();
        var transcriptionSid = form["TranscriptionSid"].ToString();
        var recordingUrl = form["RecordingUrl"].ToString();
        var callingNumber = form["From"].ToString();
        
        _logger.LogInformation("Transcription details -> TranscriptionSid: [{transcriptionSid}], RecordingSid: [{recordingSid}], RecordingUrl: [{recordingUrl}]", 
            transcriptionSid, recordingSid, recordingUrl);

        var transcriptionResource = await TranscriptionResource.FetchAsync(
            pathRecordingSid: recordingSid,
            pathSid: transcriptionSid,
            client: _twilioClient
        );
        _logger.LogInformation("Transcription text: {transcriptionText}", transcriptionResource.TranscriptionText);
        
        var httpClient = _httpClientFactory.CreateClient();
        var recordingBytes = await httpClient.GetByteArrayAsync($"{recordingUrl}.mp3");
        
        var from = new EmailAddress("{your sender email}", "{your sender display name}");
        var to = new EmailAddress("{your recipient email}", "{your recipient display name}");
        var subject = "You've got voicemail!";
        var plainTextContent = $"Calling Number: {callingNumber}{Environment.NewLine}Transcription: {transcriptionResource.TranscriptionText}";
        var htmlContent = $"<p>Calling Number: {callingNumber}</p><p>Transcription: {transcriptionResource.TranscriptionText}</p>";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        msg.AddAttachment(
            new Attachment
            {
                Content = Convert.ToBase64String(recordingBytes),
                Filename = "voicemail.mp3",
                Type = "audio/mpeg",
                Disposition = "attachment"
            });
        
        var sendEmailResponse = await _sendGridClient.SendEmailAsync(msg);
        Console.WriteLine(sendEmailResponse.IsSuccessStatusCode ? "Email queued successfully!" : "Something went wrong!");
    }
}

