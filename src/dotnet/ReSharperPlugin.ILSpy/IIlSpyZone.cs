using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.ILSpy;

[ZoneDefinition]
// [ZoneDefinitionConfigurableFeature("Title", "Description", IsInProductSection: false)]
public interface IIlSpyZone : IZone, IRequire<ILanguageCSharpZone>, IRequire<ExternalSourcesZone>;