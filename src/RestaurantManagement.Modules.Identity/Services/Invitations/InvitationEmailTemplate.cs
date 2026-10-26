using System.Globalization;
using System.Net;
using System.Reflection;
using RestaurantManagement.Modules.Identity.Services.Languages;

namespace RestaurantManagement.Modules.Identity.Services.Invitations
{
    /// <summary>
    /// The invitation email in English, Portuguese or French (English for any other language). Every value
    /// is HTML-encoded; the email shows role names and locations only, never levels, and never a password.
    /// </summary>
    public static class InvitationEmailTemplate
    {
        private sealed record Wording(string Connector, string ExpiryFormat, CultureInfo ExpiryCulture, Func<string, string, string> Subject);

        private static readonly Dictionary<string, Wording> Wordings = new()
        {
            [SupportedLanguages.English] = new("at", "MMMM d, yyyy", new CultureInfo("en-US"),
                (organizationName, productName) => $"You are invited to join {organizationName} on {productName}"),
            [SupportedLanguages.Portuguese] = new("em", "d 'de' MMMM 'de' yyyy", new CultureInfo("pt-BR"),
                (organizationName, productName) => $"Você foi convidado para {organizationName} no {productName}"),
            [SupportedLanguages.French] = new("à", "d MMMM yyyy", new CultureInfo("fr-FR"),
                (organizationName, productName) => $"Vous êtes invité à rejoindre {organizationName} sur {productName}")
        };

        private static readonly Dictionary<string, string> Templates = Wordings.Keys.ToDictionary(
            language => language, language => Load($"invitation-email-{language}.html"));

        public static string Subject(string language, string organizationName, string productName)
            => WordingFor(language).Subject(organizationName, productName);

        /// <summary>Roles are (role name, location name) pairs, one line each.</summary>
        public static string Build(
            string language, string inviterName, string organizationName, string productName,
            IEnumerable<(string RoleName, string LocationName)> roles, string acceptPageUrl, string token, DateTime expiresAt)
        {
            var wording = WordingFor(language);
            var roleLines = string.Concat(roles.Select(r =>
                $"<li>{Encode(r.RoleName)} {wording.Connector} {Encode(r.LocationName)}</li>"));

            return Templates[Wordings.ContainsKey(language) ? language : SupportedLanguages.Default]
                .Replace("{{INVITER}}", Encode(inviterName))
                .Replace("{{ORGANIZATION}}", Encode(organizationName))
                .Replace("{{PRODUCT}}", Encode(productName))
                .Replace("{{ROLES}}", roleLines)
                .Replace("{{EXPIRY}}", Encode(expiresAt.ToString(wording.ExpiryFormat, wording.ExpiryCulture) + " (UTC)"))
                .Replace("{{LINK}}", Encode(BuildLink(acceptPageUrl, token)));
        }

        private static Wording WordingFor(string language)
            => Wordings.TryGetValue(language, out var wording) ? wording : Wordings[SupportedLanguages.Default];

        private static string BuildLink(string acceptPageUrl, string token)
            => $"{acceptPageUrl}{(acceptPageUrl.Contains('?') ? '&' : '?')}token={Uri.EscapeDataString(token)}";

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private static string Load(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"RestaurantManagement.Modules.Identity.Templates.{fileName}";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found in assembly '{assembly.FullName}'.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
