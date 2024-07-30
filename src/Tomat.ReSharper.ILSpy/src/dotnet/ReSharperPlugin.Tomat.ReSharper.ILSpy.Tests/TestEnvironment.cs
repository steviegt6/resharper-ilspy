using System.Threading;

using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;

using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.Tomat.ReSharper.ILSpy.Tests;

[ZoneDefinition]
public class IlSpyTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IIlSpyZone>;

[ZoneMarker]
public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<IlSpyTestEnvironmentZone>;

[SetUpFixture]
public class IlSpyTestsAssembly : ExtensionTestEnvironmentAssembly<IlSpyTestEnvironmentZone>;