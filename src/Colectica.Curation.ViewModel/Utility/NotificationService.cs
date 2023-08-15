// Copyright 2014 - 2018 Colectica.
// 
// This file is part of the Colectica Curation Tools.
// 
// The Colectica Curation Tools are free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// The Colectica Curation Tools are distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for
// more details.
// 
// You should have received a copy of the GNU Affero General Public License along
// with Colectica Curation Tools. If not, see <https://www.gnu.org/licenses/>.

﻿using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using log4net;
using log4net.Repository.Hierarchy;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace Colectica.Curation.Web.Utility
{
    public class NotificationService
    {      
        public static void SendActionEmail(string toAddress, string toName, string fromAddress, string fromName, string subject, string paragraph1, string paragraph2, string actionText, string actionUrl, string closing, string mailPreferencesUrl, ApplicationDbContext db)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                return;
            }

            string plainBody = ReplaceTokens(GetTemplate("action.txt"),
                subject,
                string.Empty,
                paragraph1, paragraph2,
                actionText, actionUrl,
                closing, mailPreferencesUrl);

            string htmlBody = ReplaceTokens(GetTemplate("action.html"),
                subject, 
                string.Empty, 
                paragraph1, paragraph2, 
                actionText, actionUrl, 
                closing, mailPreferencesUrl);

            SendEmail(toAddress, toName, string.IsNullOrWhiteSpace(fromAddress) ? "no-reply@example.org" : fromAddress, fromName, subject, htmlBody, plainBody, db);
        }

        public static void SendAlertEmail(string toAddress, string toName, string fromAddress, string fromName, string subject, string alertText, string paragraph1, string paragraph2, string actionText, string actionUrl, string closing, string mailPreferencesUrl, ApplicationDbContext db)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                return;
            }

            string plainBody = ReplaceTokens(GetTemplate("alert.txt"),
                subject,
                alertText,
                paragraph1, paragraph2,
                actionText, actionUrl,
                closing, mailPreferencesUrl);

            string htmlBody = ReplaceTokens(GetTemplate("alert.html"),
                subject,
                alertText,
                paragraph1, paragraph2,
                actionText, actionUrl,
                closing, mailPreferencesUrl);

            SendEmail(toAddress, toName, string.IsNullOrWhiteSpace(fromAddress) ? "no-reply@example.org" : fromAddress, fromName, subject, htmlBody, plainBody, db);
        }

        static void SendEmail(string toAddress, string toName, string fromAddress, string fromName, string subject, string htmlBody, string plainBody, ApplicationDbContext db)
        {
            // Build the mail message.
            var from = new MailboxAddress(fromName, fromAddress);
            var to = new MailboxAddress(toName, toAddress);
            var mail = new MimeMessage();
            mail.From.Add(from);
            mail.To.Add(to);


            var builder = new BodyBuilder();
            if (!string.IsNullOrWhiteSpace(plainBody))
            {
                builder.TextBody = plainBody;
            }
            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                builder.HtmlBody = htmlBody;
            }

            mail.Subject = subject;
            mail.Body = builder.ToMessageBody();

            // Configure the server.
            var settings = GetSiteSettings(db);
            if (settings == null ||
                string.IsNullOrWhiteSpace(settings.SmtpHost))
            {
                return;
            }

            // Send the mail.
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(settings.SmtpHost, settings.SmtpPort);

                    if (!string.IsNullOrWhiteSpace(settings.SmtpUserName) &&
                        !string.IsNullOrWhiteSpace(settings.SmtpPassword))
                    {
                        client.Authenticate(settings.SmtpUserName, settings.SmtpPassword);
                    }

                    client.Send(mail);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                ILog logger = LogManager.GetLogger("Curation");
                logger.Error("Error sending email.", ex);
            }
        }


        static string ReplaceTokens(string str, string subject, string alertText, string paragraph1, string paragraph2, string actionText, string actionUrl, string closing, string mailPreferencesUrl)
        {
            return str
                .Replace("@subject", subject)
                .Replace("@alertText", alertText)
                .Replace("@paragraph1", paragraph1)
                .Replace("@paragraph2", paragraph2)
                .Replace("@actionUrl", actionUrl)
                .Replace("@actionText", actionText)
                .Replace("@closing", closing)
                .Replace("@mailPreferencesUrl", mailPreferencesUrl);
        }

        static string GetTemplate(string templateName)
        {
            var assembly = typeof(NotificationService).Assembly;
            var resourceName = "Colectica.Curation.Common.Resources.email.inlined." + templateName;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();
                return content;
            }
        }

        public static void SendDecisionNotification(bool approved, ApplicationUser notifyUser, CatalogRecord record, Organization org, string urlBase, ApplicationDbContext db)
        {
            SendActionEmail(
                notifyUser.Email, notifyUser.FullName,
                org.ReplyToAddress, org.Name,
                string.Format("[Catalog Record Publication {0}] {1}", approved ? "Approved" : "Rejected", record.Title),
                string.Format("The request to publish {0} has been {1}.", record.Title, approved ? "approved" : "rejected"),
                "Click below to open the record.",
                "Open " + record.Title,
                urlBase + "/CatalogRecord/General/" + record.Id.ToString(),
                org.NotificationEmailClosing,
                urlBase + "/User/EmailPreferences/" + notifyUser.UserName,
                db);
        }

        static SiteSettings GetSiteSettings(ApplicationDbContext db)
        {
            var settingsRow = db.Settings.Where(x => x.Name == "SiteSettings").FirstOrDefault();
            if (settingsRow != null)
            {
                return JsonConvert.DeserializeObject<SiteSettings>(settingsRow.Value);
            }
            else
            {
                return new SiteSettings();
            }
        }


    }
}
