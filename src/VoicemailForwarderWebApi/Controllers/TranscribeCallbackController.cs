using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.AspNet.Core;
using Twilio.Rest.Api.V2010.Account.Recording;

namespace TwilioWebhookLambda.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TranscribeCallbackController : TwilioController
{
    private readonly ILogger<TranscribeCallbackController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISendGridClient _sendGridClient;
    
    public TranscribeCallbackController(ILogger<TranscribeCallbackController> logger, IHttpClientFactory httpClientFactory, ISendGridClient sendGridClient)
    {
        _logger = logger;
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
        _logger.LogInformation("Transcription details -> TranscriptionSid: [{transcriptionSid}], RecordingSid: [{recordingSid}], RecordingUrl: [{recordingUrl}]", 
            transcriptionSid, recordingSid, recordingUrl);
        
        var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        TwilioClient.Init(accountSid, authToken);

        var transcriptionResource = await TranscriptionResource.FetchAsync(
            pathRecordingSid: recordingSid,
            pathSid: transcriptionSid
        );
        _logger.LogInformation("Transcription text: {transcriptionText}", transcriptionResource.TranscriptionText);
        
        var recordingFilePath = $"{recordingUrl.Substring(recordingUrl.LastIndexOf("/") + 1)}.mp3";
        var httpClient = _httpClientFactory.CreateClient();
        
        var recordingBytes = await httpClient.GetByteArrayAsync($"{recordingUrl}.mp3");
        System.IO.File.WriteAllBytes(recordingFilePath, recordingBytes);

        var from = new EmailAddress("{your sender email}", "{your sender display name}");
        var to = new EmailAddress("{your recipient email}", "{your recipient display name}");
        
        var subject = "You've got voicemail!";
        var plainTextContent = transcriptionResource.TranscriptionText;
        var htmlContent = transcriptionResource.TranscriptionText;
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
        
        System.IO.File.Delete(recordingFilePath);
    }
}

