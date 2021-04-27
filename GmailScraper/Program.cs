using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.WebUtilities;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace GmailScraper
{
    internal static class Program
    {
        private const RegexOptions RegExOptions =
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline;
        private const string ApplicationName = "CGC - Find Emails";

        public static List<FieldToFind> FieldsList = new List<FieldToFind>();

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        private static readonly string[] Scopes = {GmailService.Scope.GmailReadonly};
        private static readonly Encoding Utf8 = Encoding.UTF8;
        private static readonly GmailService GmailService;
        public static bool Stop = true;

        static Program()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                //careful
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                const string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Gmail API service.
            GmailService = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }


        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }


        public static async Task<List<string[]>> GetEmailsAndScrapeThem(string search,
            Action<string[]> onAdd)
        {
            var rows = new List<string[]>();
            UsersResource.MessagesResource.ListRequest request =
                GmailService.Users.Messages.List("me");
            request.MaxResults = int.MaxValue;
            request.Q = search;
            IList<Message> messages = (await request.ExecuteAsync()).Messages;
            if (messages == null || messages.Count <= 0) return rows;
            try
            {
                foreach (Message message in messages)
                {
                    if (Stop)
                    {
                        break;
                    }

                    UsersResource.MessagesResource.GetRequest req =
                        GmailService.Users.Messages.Get("me", message.Id);
                    Message mes = await req.ExecuteAsync();
                    string a;
                    if (!string.IsNullOrEmpty(mes.Payload.Parts[0].Body.Data))
                    {
                        a = mes.Payload.Parts[0].Body.Data;
                    }
                    else if (mes.Payload.Parts[0].Parts[0].Parts != null)
                    {
                        a = mes.Payload.Parts[0].Parts[0].Parts[1].Body.Data;
                    }
                    else
                    {
                        a = mes.Payload.Parts[0].Parts[1].Body.Data;
                    }

                    if (a == null) continue;
                    string body = Utf8.GetString(WebEncoders.Base64UrlDecode(a));
                    var results = new string[FieldsList.Count];
                    for (var i = 0; i < FieldsList.Count; i++)
                    {
                        FieldToFind f = FieldsList[i];
                        Match rcMatch = Regex.Match(body, f.Regex, RegExOptions);
                        if (f.Required && !rcMatch.Success)
                        {
                            results[i] = "NOT FOUND";
                        }
                        else
                        {
                            results[i] = rcMatch.Groups[f.GroupNumber].Value;
                        }
                    }

                    onAdd(results);
                    rows.Add(results);
                }
            }
            catch
            {
                // ignored
            }

            return rows;
        }
    }
}