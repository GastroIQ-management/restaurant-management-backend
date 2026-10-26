using System.Net;
using System.Reflection;

namespace RestaurantManagement.Shared.Services.Email
{
    // One email that carries a code, in every language it has text for; any other language gets English.
    internal sealed class LocalizedCodeEmail
    {
        public const string DefaultLanguage = "en";

        private const string DefaultGreetingName = "there";
        private const string FirstNameToken = "{{FIRST_NAME}}";
        private const string CodeToken = "{{CODE}}";
        private const string ExpiryMinutesToken = "{{EXPIRY_MINUTES}}";
        private const string EmailToken = "{{EMAIL}}";

        private readonly Dictionary<string, string> _subjects;
        private readonly Dictionary<string, string> _templates;

        // Each subject is paired with a template file: {baseName}.html for English, {baseName}-{language}.html for the others.
        public LocalizedCodeEmail(string baseName, Dictionary<string, string> subjectsByLanguage)
        {
            _subjects = subjectsByLanguage;
            _templates = subjectsByLanguage.Keys.ToDictionary(
                language => language,
                language => LoadTemplate(language == DefaultLanguage ? $"{baseName}.html" : $"{baseName}-{language}.html"));
        }

        public string Subject(string language) => _subjects[Resolve(language)];

        public string Build(string to, string firstName, string code, int expiryMinutes, string language)
        {
            var greetingName = string.IsNullOrWhiteSpace(firstName) ? DefaultGreetingName : firstName.Trim();

            return _templates[Resolve(language)]
                .Replace(FirstNameToken, WebUtility.HtmlEncode(greetingName))
                .Replace(CodeToken, code)
                .Replace(ExpiryMinutesToken, expiryMinutes.ToString())
                .Replace(EmailToken, WebUtility.HtmlEncode(to));
        }

        private string Resolve(string language) => _templates.ContainsKey(language) ? language : DefaultLanguage;

        private static string LoadTemplate(string fileName)
        {
            var resourceName = $"RestaurantManagement.Shared.Templates.{fileName}";
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found in assembly '{assembly.FullName}'.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
