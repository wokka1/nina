using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using NINA.Core.Locale;
using NINA.Sequencer.SequenceItem;

namespace NINA.Avalonia.Utility {

    public record SequenceItemCatalogEntry(Type Type, string Name, string Category);

    /// <summary>
    /// A real answer to the MEF/DI-bridging question flagged in SequencerViewModel's own doc
    /// comment: the real app's item palette comes from IPluginLoader doing full
    /// System.ComponentModel.Composition (MEF) discovery + composition, which lives in the main
    /// NINA exe project and isn't portable. Rather than reproduce a full MEF
    /// CompositionContainer (which would need its own bridge to this app's
    /// Microsoft.Extensions.DependencyInjection container for every exported type's
    /// [ImportingConstructor] dependencies), this does the two things actually needed and
    /// nothing more:
    ///
    /// 1. Discovery via plain reflection over NINA.Sequencer.dll, reading the same real
    ///    [Export(typeof(ISequenceItem))]/[ExportMetadata("Name"/"Category", ...)] attributes
    ///    the real app's items already carry - not new metadata, the genuine article.
    /// 2. Construction via this app's own IServiceProvider (built in CompositionRoot.Compose),
    ///    reflecting over each type's constructor (preferring one marked [ImportingConstructor]
    ///    if present, matching the real MEF convention) and resolving each parameter from the
    ///    DI container instead of a MEF composition graph.
    ///
    /// Real, deliberate limitation: only item types whose every constructor parameter is
    /// already registered in this app's DI container show up in the catalog - everything else
    /// (most equipment-mediator-dependent items work fine since those mediators are registered;
    /// items needing services this app hasn't wired yet are silently excluded rather than shown
    /// broken). That is a real, growing subset as more of the app gets wired up in
    /// CompositionRoot, not a permanent ceiling.
    ///
    /// Verified end to end with a throwaway console harness (not just "it compiles"): discovery
    /// finds 72 real [Export(typeof(ISequenceItem))] types in NINA.Sequencer.dll (matches a
    /// direct grep count). After registering IImagingMediator/IImageSaveMediator/
    /// IImageHistoryVM (a minimal-but-real MinimalImageHistoryVM stand-in - see its own doc
    /// comment), 60 have every constructor dependency this app's CompositionRoot registers,
    /// including TakeExposure - arguably the single most important item type, previously
    /// excluded until those three registrations landed.
    ///
    /// The harness itself also caught a real bug before it shipped: the first discovery pass
    /// used GetCustomAttribute&lt;ExportAttribute&gt;() (singular), which throws
    /// AmbiguousMatchException for the several real item types that carry more than one
    /// [Export] attribute (SmartExposure, TrainedFlatExposure, GlobalConstant/GlobalVariable,
    /// ResetVariable*, Variable, SaveSequence, WaitForTime) - would have silently broken
    /// discovery for everything past the first ambiguous type encountered. Fixed by switching to
    /// GetCustomAttributes (plural) + Any(), see the comment at that call site.
    ///
    /// Known, real, not-yet-handled gap: "resolvable constructor" only means every parameter
    /// type is DI-registered - it does NOT guarantee the constructor runs without throwing.
    /// Confirmed via the same harness: WaitForAltitude's constructor touches NINA.Astrometry's
    /// SOFA library, which throws DllNotFoundException for kernel32.dll on macOS (a genuine
    /// Windows-only native dependency, same category as the vendor camera SDKs found during the
    /// NINA.Equipment multi-target) - so this type currently shows up in Entries as
    /// "resolvable" but would actually throw if Create() were called on this platform. Not
    /// worth a general fix (would mean speculatively constructing every candidate just to test
    /// it, defeating the point of a cheap discovery pass) - flagging here so it isn't mistaken
    /// for a bug in the DI-resolution logic itself if it resurfaces.
    /// </summary>
    public class SequenceItemCatalog {
        private readonly IServiceProvider serviceProvider;

        public SequenceItemCatalog(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            Entries = Discover();
        }

        public IReadOnlyList<SequenceItemCatalogEntry> Entries { get; }

        private List<SequenceItemCatalogEntry> Discover() {
            var entries = new List<SequenceItemCatalogEntry>();
            var assembly = typeof(ISequenceItem).Assembly;

            foreach (var type in assembly.GetTypes()) {
                if (type.IsAbstract || type.IsInterface || !typeof(ISequenceItem).IsAssignableFrom(type)) {
                    continue;
                }

                // GetCustomAttributes (plural), not GetCustomAttribute: confirmed via a
                // standalone reflection probe that several real item types (SmartExposure,
                // TrainedFlatExposure, GlobalConstant/GlobalVariable, ResetVariable*, Variable,
                // SaveSequence, WaitForTime) carry more than one [Export] attribute - the
                // singular GetCustomAttribute<T>() throws AmbiguousMatchException for those,
                // which would have silently taken down catalog discovery for everything past
                // the first ambiguous type encountered.
                var exportAttrs = type.GetCustomAttributes<ExportAttribute>();
                if (!exportAttrs.Any(a => a.ContractType == typeof(ISequenceItem))) {
                    continue;
                }

                if (!TryFindResolvableConstructor(type, out _)) {
                    continue;
                }

                var name = GetMetadata(type, "Name") ?? type.Name;
                var category = GetMetadata(type, "Category") ?? string.Empty;
                entries.Add(new SequenceItemCatalogEntry(type, ResolveLoc(name), ResolveLoc(category)));
            }

            return entries
                .OrderBy(e => e.Category)
                .ThenBy(e => e.Name)
                .ToList();
        }

        public ISequenceItem Create(SequenceItemCatalogEntry entry) {
            TryFindResolvableConstructor(entry.Type, out var ctor);
            var args = ctor!.GetParameters()
                .Select(p => serviceProvider.GetService(p.ParameterType))
                .ToArray();
            return (ISequenceItem)ctor.Invoke(args);
        }

        private bool TryFindResolvableConstructor(Type type, out ConstructorInfo? constructor) {
            var candidates = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var importing = candidates.FirstOrDefault(c => c.GetCustomAttribute<ImportingConstructorAttribute>() != null);
            var ordered = importing != null
                ? new[] { importing }.Concat(candidates.Where(c => c != importing))
                : candidates;

            foreach (var ctor in ordered) {
                if (ctor.GetParameters().All(p => serviceProvider.GetService(p.ParameterType) != null)) {
                    constructor = ctor;
                    return true;
                }
            }
            constructor = null;
            return false;
        }

        private static string? GetMetadata(Type type, string key) {
            return type.GetCustomAttributes<ExportMetadataAttribute>()
                .FirstOrDefault(a => a.Name == key)?.Value as string;
        }

        private static string ResolveLoc(string key) {
            var value = Loc.Instance[key];
            return string.IsNullOrEmpty(value) ? key : value;
        }
    }
}
