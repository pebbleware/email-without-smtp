import System;
import System.Mime;
import System.Diagnostics;
import System.Net.Mail;

try {
    var args = Environment.GetCommandLineArgs();
    if (args.length >= 2) {
        var recipients = new MailAddressCollection();
        recipients.Add(getArgumentValue(args, "r")); // To
        var from = new MailAddress(getArgumentValue(args, "f")); // From
        var subject = getArgumentValue(args, "s"); // Subject (optional)
        var body = getArgumentValue(args, "b"); // Body (optional)
        var isBodyHTML = getArgumentValue(args, "ibh"); // Is body HTML? (optional)

        var mail = new MailMessage();
        mail.From = from;
        mail.Subject = subject;
        mail.Body = body;

        for (var recipient in recipients) {
            mail.To.Add(recipient);

            if (isBodyHTML == "true") {
                mail.IsBodyHtml = true;
            } else {
				mail.IsBodyHtml = false;
			}
            var mailDomain = recipient.Host;

            var oProcess = new System.Diagnostics.Process();
            var oStartInfo = new System.Diagnostics.ProcessStartInfo("nslookup", "-type=mx " + mailDomain);
            oStartInfo.UseShellExecute = false;
            oStartInfo.CreateNoWindow = true;
            oStartInfo.RedirectStandardOutput = true;
            oStartInfo.RedirectStandardError = true;

            oProcess.StartInfo = oStartInfo;
            oProcess.Start();

            var mxData = oProcess.StandardOutput.ReadToEnd();

            if (mxData.indexOf("mail exchanger") == -1) {
                throw new Error("MX record lookup failed.", { cause: oProcess.StandardError.ReadToEnd() });
            }
            var smtpServer = mxData.split("mail exchanger = ")[1].Trim();

            var smtpClient = new SmtpClient(smtpServer);

            try {
                smtpClient.Send(mail);
                mail.To.Clear();
            } catch (e) {
                throw new Error("Email send failed.", { cause: e.message });
            }
        }
    } else {
        throw new Error("Insufficient command line arguments.", { cause: "The 'To' and 'From' addresses are required." });
    }
} catch (e) {
    Console.Error.WriteLine("Error: " + e.message + "\r\nDescription: " + e.description + "\r\n\r\nType '" + Process.GetCurrentProcess().ProcessName + " /?' for help.");
}

function getArgumentValue(args, argName) {
for (var i = 0; i < args.length; i++) {
var arg = args[i];
if (arg.lastIndexOf("/") === 0 && arg.indexOf(":") != -1) {
var argParts = arg.substring(1).split(":");
if (argParts[0].toLowerCase() === argName.toLowerCase()) {
return argParts[1];
}
}
}
return "";
}
