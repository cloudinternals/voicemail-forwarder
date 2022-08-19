using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Core;
using Twilio.TwiML;
 
namespace TwilioWebhookLambda.WebApi.Controllers;
 
[ApiController]
[Route("[controller]")]
public class IncomingCallController : TwilioController
{
    [HttpPost]
    public TwiMLResult Index()
    {
        var response = new VoiceResponse();
        response.Say("Hello. I'm not available at the moment. Please leave a message after the beep.");
        response.Record(
            timeout: 10,
            transcribe: true,
            transcribeCallback: new Uri("/TranscribeCallback", UriKind.Absolute)
        );
        return TwiML(response);
    }
}