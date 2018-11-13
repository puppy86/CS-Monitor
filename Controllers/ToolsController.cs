using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using csmon.Models;
using csmon.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace csmon.Controllers
{
    public class ToolsController : Controller
    {
        // ReSharper disable once EmptyConstructor
        public ToolsController()
        {
        }

        public IActionResult Tps()
        {
            return View();
        }

        public IActionResult Nodes()
        {
            return View();
        }

        public IActionResult ActivityGraph()
        {
            return View(new GraphData());
        }

        public IActionResult CSRequest()
        {
            return View();
        }

        public IActionResult Dictionary()
        {
            return View();
        }

        public IActionResult Node(string id)
        {
            ViewData["id"] = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CsRequestSubmit(string address, string email, int amount)
        {
            // Check ReCaptcha
            var jsonResponse = await GetRecaptchaResponseAsync(Request.Form["g-Recaptcha-Response"]);
            dynamic jsonData = JObject.Parse(jsonResponse);

            // If check fails return user back to form
            if (jsonData.success != "true")
                return View("CSRequest");            

            // Send email to support
            var msg = new StringBuilder();
            msg.AppendLine($"A Request for CS has been submitted at {Request.Host}");
            msg.AppendLine();
            msg.AppendLine($"Network: {Network.GetById(RouteData.Values["network"].ToString()).Title}");
            msg.AppendLine($"Address: {address}");
            msg.AppendLine($"Email: {email}");
            msg.AppendLine($"Amount: {amount} CS");
            msg.AppendLine();
            await SendEmailAsync(Config.EmailToAddress, "Request CS", msg.ToString());

            // Show success message
            return View("CsRequestConfirm");
        }

        public static async Task<string> GetRecaptchaResponseAsync(string reCaptchaResponse)
        {
            var uri = $"https://www.google.com/recaptcha/api/siteverify?secret={Config.RecaptchaKey}&response={reCaptchaResponse}";
            var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(uri);
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient(Config.EmailFromHost, Config.EmailFromPort)
            {
                UseDefaultCredentials = false,
                EnableSsl = true,
                Credentials = new NetworkCredential(Config.EmailFromAddress, Config.EmailFromKey)
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress(Config.EmailFromAddress)
            };
            mailMessage.To.Add(email);
            mailMessage.Subject = subject;
            mailMessage.Body = htmlMessage;
            return client.SendMailAsync(mailMessage);
        }
    }
}
