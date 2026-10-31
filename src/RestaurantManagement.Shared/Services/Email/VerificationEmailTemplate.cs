using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RestaurantManagement.Shared.Services.Email
{
    // The verification email: one layout, texts per language (en, pt, fr); any other language gets English.
    public static partial class VerificationEmailTemplate
    {
        private const string DefaultLanguage = "en";
        private const string DefaultGreetingName = "there";

        private static readonly string Layout = LoadLayout();

        private static readonly Dictionary<string, Dictionary<string, string>> Texts = new()
        {
            ["en"] = new()
            {
                ["subject"] = "Your GastroIQ code: {{code}}",
                ["preheader"] = "Use this code to confirm your email. It expires in {{minutes}} minutes.",
                ["heading"] = "Confirm your email",
                ["greeting"] = "Hi {{first_name}},",
                ["intro"] = "Enter the code below in GastroIQ to finish setting up your account.",
                ["expiry"] = "The code expires in {{minutes}} minutes.",
                ["ignore_note"] = "Didn't create an account? You can ignore this email. Nothing will happen.",
                ["signoff"] = "The GastroIQ team",
                ["help_line"] = "Questions? Just reply to this email.",
                ["sent_to_line"] = "Sent to {{email}} because an account was created with this address.",
                ["company_line"] = "© {{year}} GastroIQ"
            },
            ["pt"] = new()
            {
                ["subject"] = "Seu código do GastroIQ: {{code}}",
                ["preheader"] = "Use este código para confirmar seu e-mail. Ele vale por {{minutes}} minutos.",
                ["heading"] = "Confirme seu e-mail",
                ["greeting"] = "Olá, {{first_name}},",
                ["intro"] = "Digite o código abaixo no GastroIQ para terminar de configurar sua conta.",
                ["expiry"] = "O código vale por {{minutes}} minutos.",
                ["ignore_note"] = "Não criou uma conta? Pode ignorar este e-mail. Nada vai acontecer.",
                ["signoff"] = "Equipe GastroIQ",
                ["help_line"] = "Dúvidas? É só responder este e-mail.",
                ["sent_to_line"] = "Enviado para {{email}} porque uma conta foi criada com este endereço.",
                ["company_line"] = "© {{year}} GastroIQ"
            },
            ["fr"] = new()
            {
                ["subject"] = "Votre code GastroIQ : {{code}}",
                ["preheader"] = "Utilisez ce code pour confirmer votre courriel. Il expire dans {{minutes}} minutes.",
                ["heading"] = "Confirmez votre courriel",
                ["greeting"] = "Bonjour {{first_name}},",
                ["intro"] = "Entrez le code ci-dessous dans GastroIQ pour terminer la configuration de votre compte.",
                ["expiry"] = "Le code expire dans {{minutes}} minutes.",
                ["ignore_note"] = "Vous n’avez pas créé de compte? Vous pouvez ignorer ce courriel. Rien ne se passera.",
                ["signoff"] = "L’équipe GastroIQ",
                ["help_line"] = "Des questions? Répondez simplement à ce courriel.",
                ["sent_to_line"] = "Envoyé à {{email}}, car un compte a été créé avec cette adresse.",
                ["company_line"] = "© {{year}} GastroIQ"
            }
        };

        public static string Subject(string language, string code)
            => Fill(Texts[Resolve(language)]["subject"], new() { ["code"] = WebUtility.HtmlEncode(code) });

        public static string Build(string to, string firstName, string code, int expiryMinutes, string language, string logoUrl = "")
        {
            var resolved = Resolve(language);
            var greetingName = string.IsNullOrWhiteSpace(firstName) ? DefaultGreetingName : firstName.Trim();

            var values = new Dictionary<string, string>
            {
                ["code"] = WebUtility.HtmlEncode(code),
                ["minutes"] = expiryMinutes.ToString(),
                ["first_name"] = WebUtility.HtmlEncode(greetingName),
                ["email"] = WebUtility.HtmlEncode(to),
                ["year"] = DateTime.UtcNow.Year.ToString()
            };

            // Each text is filled on its own and the layout in one pass, so a value is never read as a placeholder.
            var fields = Texts[resolved].ToDictionary(text => text.Key, text => Fill(text.Value, values));
            fields["locale"] = resolved;
            fields["code"] = values["code"];
            fields["logo_url"] = WebUtility.HtmlEncode(logoUrl);

            return Fill(Layout, fields);
        }

        private static string Resolve(string language) => Texts.ContainsKey(language) ? language : DefaultLanguage;

        private static string Fill(string template, Dictionary<string, string> values)
            => PlaceholderPattern().Replace(template, match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);

        [GeneratedRegex(@"\{\{(\w+)\}\}")]
        private static partial Regex PlaceholderPattern();

        private static string LoadLayout()
        {
            const string resourceName = "RestaurantManagement.Shared.Templates.verification-email.html";
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found in assembly '{assembly.FullName}'.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
