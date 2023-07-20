using System;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;

namespace qsm;

class Program
{
    static void Main(string[] args)
    {
        var toOption = new Option<string>("--to", "A comma-separated list of addresses the email will be sent to.");
        toOption.AddAlias("-t");
        
        var fromOption = new Option<string>("--from", "The address the email will be sent from");
        fromOption.AddAlias("-f");
        
        var nameOption = new Option<string>("--name", "Name of the sender");
        nameOption.AddAlias("-n");
        
        var replyToOption = new Option<string>("--replyto", "A comma-separated list of addresses for the Reply-To");
        replyToOption.AddAlias("-r");
        
        var ccOption = new Option<string>("--cc", "A comma-separated list of addresses for the CC");
        ccOption.AddAlias("-c");
        
        var bccOption = new Option<string>("--bcc", "A comma-separated list of addresses for the BCC");
        bccOption.AddAlias("-b");
        
        var subjectOption = new Option<string>("--subject", "The subject of the email");
        subjectOption.AddAlias("-s");
        
        var bodyOption = new Option<string>("--body", "The body of the email");
        bodyOption.AddAlias("-m");
        
        var attachmentsOption = new Option<string>("--attachments", "A comma-separated list of file names to be added as attachments to the email");
        attachmentsOption.AddAlias("-a");
        
        var bodyIsHtmlOption = new Option<bool>("--bodyishtml", "A boolean (true/false) indicating whether or not the body is HTML");

        var rootCommand = new RootCommand
        {
            toOption,
            fromOption,
            nameOption,
            replyToOption,
            ccOption,
            bccOption,
            subjectOption,
            bodyOption,
            attachmentsOption,
            bodyIsHtmlOption
        };

        rootCommand.Handler = CommandHandler.Create<string, string, string, string, string, string, string, string, string, bool>(SendEmail);

        rootCommand.Invoke(args);
    }

    private static void SendEmail(string to, string from, string name, string replyTo, string cc, string bcc, string subject, string body, string attachments, bool bodyIsHtml)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(to))
            {
                var recipients = new MailAddressCollection();
                recipients.Add(to); // To

                var sender = new MailAddress(from ?? "user@" + Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString(), name); // From (optional)

                var mail = new MailMessage();
                mail.From = sender;
                if (!string.IsNullOrWhiteSpace(replyTo))
                {
                    mail.ReplyToList.Add(replyTo);
                }
                if (!string.IsNullOrWhiteSpace(cc))
                {
                    mail.CC.Add(cc);
                }
                if (!string.IsNullOrWhiteSpace(bcc))
                {
                    mail.Bcc.Add(bcc);
                }
                mail.Subject = subject;
                mail.Body = body;
                if (!string.IsNullOrWhiteSpace(attachments))
                {
                    foreach (var att in attachments.Split(','))
                    {
                        mail.Attachments.Add(new Attachment(att));
                    }
                }
				
				if (bodyIsHtml)
                {
                    mail.IsBodyHtml = true;
                }
                else
                {
                    mail.IsBodyHtml = false;
                }

                foreach (var recipient in recipients)
                {
                    mail.To.Add(recipient);

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

                    if (mxData.IndexOf("mail exchanger") == -1)
                    {
                        throw new Exception("MX record lookup failed. " + oProcess.StandardError.ReadToEnd());
                    }

                    var smtpServer = mxData.Split(new[] { "mail exchanger = " }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();

                    var smtpClient = new SmtpClient(smtpServer);

                    try
                    {
                        smtpClient.Send(mail);
                        mail.To.Clear();
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Email send failed. " + e.Message);
                    }
                }
            }
            else
            {
                throw new Exception("Missing arguments.");
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Error: " + e.Message);
        }
    }
}
