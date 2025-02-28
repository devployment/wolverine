using JasperFx.CodeGeneration;
using Lamar;
using Wolverine.Configuration;

namespace Wolverine.Persistence.Sagas;

public static class GenerationRulesExtensions
{
    public static readonly string PersistenceKey = "PERSISTENCE";

    private static readonly IPersistenceFrameProvider _nullo = new InMemoryPersistenceFrameProvider();

    /// <summary>
    ///     The currently known strategy for code generating transaction middleware
    /// </summary>
    public static void AddPersistenceStrategy<T>(this GenerationRules rules) where T : IPersistenceFrameProvider, new()
    {
        if (rules.Properties.TryGetValue(PersistenceKey, out var raw) && raw is List<IPersistenceFrameProvider> list)
        {
            if (!list.OfType<T>().Any())
            {
                list.Add(new T());
            }
        }
        else
        {
            list = new List<IPersistenceFrameProvider>();
            list.Add(new T());
            rules.Properties[PersistenceKey] = list;
        }
    }

    public static List<IPersistenceFrameProvider> PersistenceProviders(this GenerationRules rules)
    {
        if (rules.Properties.TryGetValue(PersistenceKey, out var raw) &&
            raw is List<IPersistenceFrameProvider> list)
        {
            return list;
        }

        return new List<IPersistenceFrameProvider>();
    }

    /// <summary>
    ///     The currently known strategy for code generating transaction middleware
    /// </summary>
    public static IPersistenceFrameProvider GetPersistenceProviders(this GenerationRules rules, IChain chain,
        IContainer container)
    {
        if (rules.Properties.TryGetValue(PersistenceKey, out var raw) && raw is List<IPersistenceFrameProvider> list)
        {
            return list.FirstOrDefault(x => x.CanApply(chain, container)) ?? _nullo;
        }

        return _nullo;
    }
}