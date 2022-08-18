using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace VoicemailForwarderWebApi.Controllers;

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
        var recordingUrl = form["RecordingUrl"].ToString();
        var transcriptionText = form["TranscriptionText"].ToString();
        var callingNumber = form["From"].ToString();

        _logger.LogInformation("Transcription details -> CallingNumber: [{callingNumber}] TranscriptionText: [{transcriptionText}], RecordingSid: [{recordingSid}], RecordingUrl: [{recordingUrl}]", 
            callingNumber, transcriptionText, recordingSid, recordingUrl);

        var httpClient = _httpClientFactory.CreateClient();
        var recordingBytes = await httpClient.GetByteArrayAsync($"{recordingUrl}.mp3");

        var from = new EmailAddress("{your sender email}", "{your sender display name}");
        var to = new EmailAddress("{your recipient email}", "{your recipient display name}");
        var subject = "You've got voicemail!";
        var plainTextContent = $"Calling Number: {callingNumber}{Environment.NewLine}Transcription: {transcriptionText}";
        var htmlContent = $"<p>Calling Number: {callingNumber}</p><p>Transcription: {transcriptionText}</p>";
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
        _logger.LogInformation(sendEmailResponse.IsSuccessStatusCode ? "Email queued successfully!" : "Something went wrong!");
    }
}