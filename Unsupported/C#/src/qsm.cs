using System;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

class Program {
  static void Main(string[] args) {
    try {
      if (args.Length >= 1) {
		if (args[0].ToLower() == "/?" || args[0].ToLower() == "/help" || args[0].ToLower() == "/h") {
		  Console.WriteLine(@"Pebbleware QuickSendMail Command Line Utility [https://github.com/Pebbleware/email-without-smtp]
Copyright (C) Pebbleware. All rights reserved.

Usage: qsm.wsf /to:value [/from:value] [/replyto:value] [/cc:value] [/bcc:value] [/subject:value] [/body:value] [/attachments:value] [/html:value]

Options:

to          : A comma-separated list of addresses the email will be sent to.
from        : The address the email will be sent from
replyto     : A comma-separated list of addresses for the Reply-To
cc          : A comma-separated list of addresses for the CC
bcc         : A comma-separated list of addresses for the BCC
subject     : The subject of the email
body        : The body of the email
attachments : A comma-separated list of file names to be added as attachments to the email
html        : A boolean (true/false) indicating whether or not the body is HTML
Example     : qsm.wsf /to:sam@example.com /from:ben@example.org /subject:Greetings ""/body:Hello World!""
");
		}
        if (!string.IsNullOrWhiteSpace(GetArgumentValue(args, "to")) || !string.IsNullOrWhiteSpace(GetArgumentValue(args, "t"))) {
          var recipients = new MailAddressCollection();
          recipients.Add(GetArgumentValue(args, "to") ?? GetArgumentValue(args, "t")); // To

          var sender = new MailAddress(GetArgumentValue(args, "from") ?? GetArgumentValue(args, "f") ?? "user@" + Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString() ?? "", GetArgumentValue(args, "name") ?? GetArgumentValue(args, "n")); // From
          var replyTo = GetArgumentValue(args, "replyto") ?? GetArgumentValue(args, "r"); // Reply To (optional)
          var cc = GetArgumentValue(args, "bcc"); // CC (optional)
          var bcc = GetArgumentValue(args, "bcc"); // BCC (optional)
          var subject = GetArgumentValue(args, "subject") ?? GetArgumentValue(args, "s"); // Subject (optional)
          var body = GetArgumentValue(args, "body") ?? GetArgumentValue(args, "b"); // Body (optional)
          var attachments = GetArgumentValue(args, "attachments") ?? GetArgumentValue(args, "a"); // Attachments (optional)
          var isBodyHTML = GetArgumentValue(args, "html"); // Is body HTML? (optional)

          var mail = new MailMessage();
          mail.From = sender;
          if (!string.IsNullOrWhiteSpace(replyTo)) {
            mail.ReplyToList.Add(replyTo);
          }
          if (!string.IsNullOrWhiteSpace(cc)) {
            mail.CC.Add(cc);
          }
          if (!string.IsNullOrWhiteSpace(bcc)) {
            mail.Bcc.Add(bcc);
          }
          mail.Subject = subject;
          mail.Body = body;
          if (!string.IsNullOrWhiteSpace(attachments)) {
            foreach(var att in attachments.Split(',')) {
              mail.Attachments.Add(new Attachment(att));
            }
          }

          foreach(var recipient in recipients) {
            mail.To.Add(recipient);

            if (isBodyHTML == "true") {
              mail.IsBodyHtml = true;
            } else {
              mail.IsBodyHtml = false;
            }

            var mailDomain = recipient.Host;

            var oProcess = new Process();
            var oStartInfo = new ProcessStartInfo("nslookup", "-type=mx " + mailDomain);
            oStartInfo.UseShellExecute = false;
            oStartInfo.CreateNoWindow = true;
            oStartInfo.RedirectStandardOutput = true;
            oStartInfo.RedirectStandardError = true;

            oProcess.StartInfo = oStartInfo;
            oProcess.Start();

            var mxData = oProcess.StandardOutput.ReadToEnd();

            if (mxData.IndexOf("mail exchanger") == -1) {
              throw new Exception("MX record lookup failed. " + oProcess.StandardError.ReadToEnd());
            }

            var smtpServer = mxData.Split(new [] {
              "mail exchanger = "
            }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();

            var smtpClient = new SmtpClient(smtpServer);

            try {
              smtpClient.Send(mail);
              mail.To.Clear();
            } catch (Exception e) {
              throw new Exception("Email send failed. " + e.Message);
            }
          }
        } else {
          throw new Exception("Invalid arguments.");
        }
      } else {
        throw new Exception("Insufficient arguments. The 'From' address is required.");
      }
    } catch (Exception e) {
      Console.Error.WriteLine("Error: " + e.Message + "\r\n\r\nType '" + Process.GetCurrentProcess().ProcessName + " /?' for help.");
    }
  }

  static string GetArgumentValue(string[] args, string argName) {
    foreach(var arg in args) {
      if (arg.StartsWith("/") && arg.Contains(":")) {
        var argParts = arg.Substring(1).Split(':');
        if (argParts[0].ToLower() == argName.ToLower()) {
          return argParts[1];
        }
      }
    }
    return null;
  }
}