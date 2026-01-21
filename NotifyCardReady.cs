//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Identity.UI.Services;
//using System.Threading.Tasks;

//namespace StudentConnect.Controllers
//{
//    public class CardController : Controller
//    {
//        private readonly IEmailSender _emailSender;

//        public CardController(IEmailSender emailSender)
//        {
//            _emailSender = emailSender;
//        }

//        [HttpPost]
//        public async Task<IActionResult> NotifyCardReady(string studentEmail)
//        {
//            if (string.IsNullOrWhiteSpace(studentEmail))
//                return BadRequest("Student email is required.");

//            var subject = "Your Student Card is Ready for Collection";
//            var message = @"Dear Student,<br/><br/>
//                            Your student card is ready for collection at the admin office.<br/>
//                            Please bring your ID when collecting.<br/><br/>
//                            Regards,<br/>StudentConnect Admin";

//            await _emailSender.SendEmailAsync(studentEmail, subject, message);

//            return Ok("Notification email sent successfully.");
//        }
//    }
//}
