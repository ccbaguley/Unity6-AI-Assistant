#if UNITY_EDITOR
using System.Collections.Generic;

namespace ClippyAssistant
{
    public interface IAssistantBackend
    {
        System.Collections.Generic.List<ClippySuggestion> Analyze(string message, string stack, string pathHint);
    }

    public class LocalRuleBackend : IAssistantBackend
    {
        public List<ClippySuggestion> Analyze(string message, string stack, string pathHint)
            => ClippyRuleEngine.GetSuggestions(message, stack, pathHint);
    }

    public static class ClippySettings
    {
        static IAssistantBackend _backend;
        public static IAssistantBackend backend => _backend ??= new LocalRuleBackend();
    }
}
#endif
